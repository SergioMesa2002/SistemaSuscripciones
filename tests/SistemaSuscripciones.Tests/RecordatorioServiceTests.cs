using SistemaSuscripciones.Application.Interfaces;
using SistemaSuscripciones.Application.Services;
using SistemaSuscripciones.Domain.Entities;
using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Tests;

/// <summary>
/// Verifica las reglas del ciclo de recordatorios sin utilizar MongoDB ni un servidor SMTP.
/// </summary>
public sealed class RecordatorioServiceTests
{
    /// <summary>
    /// Comprueba que el primer aviso avance la factura únicamente después de entregar el correo.
    /// </summary>
    [Fact]
    public async Task EnviarAsync_PrimerRecordatorio_CambiaASegundoRecordatorio()
    {
        var factura = CrearFactura(EstadoRecordatorio.PrimerRecordatorio);
        var repositorio = new FacturaRepositoryFake(factura);
        var correo = new EmailServiceFake();
        var servicio = new RecordatorioService(repositorio, correo);
        var inicio = DateTime.UtcNow;

        var resultado = await servicio.EnviarAsync(factura.Id);

        Assert.True(resultado.Exito);
        Assert.Equal(EstadoRecordatorio.SegundoRecordatorio, resultado.NuevoEstado);
        Assert.Equal(EstadoRecordatorio.SegundoRecordatorio, factura.Estado);
        Assert.Equal([EstadoRecordatorio.PrimerRecordatorio], correo.EstadosEnviados);
        Assert.Equal(1, repositorio.CantidadActualizaciones);
        Assert.NotNull(factura.UltimoCorreoEnviado);
        Assert.InRange(factura.UltimoCorreoEnviado.Value, inicio, DateTime.UtcNow);
    }

    /// <summary>
    /// Comprueba que el segundo aviso cierre el ciclo para impedir nuevos envíos.
    /// </summary>
    [Fact]
    public async Task EnviarAsync_SegundoRecordatorio_CambiaADesactivado()
    {
        var factura = CrearFactura(EstadoRecordatorio.SegundoRecordatorio);
        var repositorio = new FacturaRepositoryFake(factura);
        var correo = new EmailServiceFake();
        var servicio = new RecordatorioService(repositorio, correo);

        var resultado = await servicio.EnviarAsync(factura.Id);

        Assert.True(resultado.Exito);
        Assert.Equal(EstadoRecordatorio.Desactivado, resultado.NuevoEstado);
        Assert.Equal(EstadoRecordatorio.Desactivado, factura.Estado);
        Assert.Equal([EstadoRecordatorio.SegundoRecordatorio], correo.EstadosEnviados);
        Assert.Equal(1, repositorio.CantidadActualizaciones);
    }

    /// <summary>
    /// Comprueba que una factura cerrada no invoque correo ni persistencia.
    /// </summary>
    [Fact]
    public async Task EnviarAsync_Desactivado_NoEnviaNiActualiza()
    {
        var factura = CrearFactura(EstadoRecordatorio.Desactivado);
        var repositorio = new FacturaRepositoryFake(factura);
        var correo = new EmailServiceFake();
        var servicio = new RecordatorioService(repositorio, correo);

        var resultado = await servicio.EnviarAsync(factura.Id);

        Assert.False(resultado.Exito);
        Assert.Equal(EstadoRecordatorio.Desactivado, resultado.NuevoEstado);
        Assert.Empty(correo.EstadosEnviados);
        Assert.Equal(0, repositorio.CantidadActualizaciones);
        Assert.Null(factura.UltimoCorreoEnviado);
    }

    /// <summary>
    /// Comprueba que un fallo SMTP conserve intactos el estado y la auditoría de la factura.
    /// </summary>
    [Fact]
    public async Task EnviarAsync_CuandoCorreoFalla_NoCambiaEstadoNiPersiste()
    {
        var factura = CrearFactura(EstadoRecordatorio.PrimerRecordatorio);
        var repositorio = new FacturaRepositoryFake(factura);
        var correo = new EmailServiceFake
        {
            Excepcion = new InvalidOperationException("Fallo SMTP simulado.")
        };
        var servicio = new RecordatorioService(repositorio, correo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.EnviarAsync(factura.Id));

        Assert.Equal(EstadoRecordatorio.PrimerRecordatorio, factura.Estado);
        Assert.Equal([EstadoRecordatorio.PrimerRecordatorio], correo.EstadosEnviados);
        Assert.Equal(0, repositorio.CantidadActualizaciones);
        Assert.Null(factura.UltimoCorreoEnviado);
    }

    /// <summary>
    /// Comprueba que un identificador desconocido produzca un resultado controlado sin efectos externos.
    /// </summary>
    [Fact]
    public async Task EnviarAsync_FacturaInexistente_DevuelveResultadoFallido()
    {
        var repositorio = new FacturaRepositoryFake();
        var correo = new EmailServiceFake();
        var servicio = new RecordatorioService(repositorio, correo);

        var resultado = await servicio.EnviarAsync("no-existe");

        Assert.False(resultado.Exito);
        Assert.Equal("no-existe", resultado.FacturaId);
        Assert.Null(resultado.NuevoEstado);
        Assert.Contains("No se encontró", resultado.Mensaje);
        Assert.Empty(correo.EstadosEnviados);
        Assert.Equal(0, repositorio.CantidadActualizaciones);
    }

    /// <summary>
    /// Comprueba que el procesamiento masivo ignore las facturas cuyo ciclo ya terminó.
    /// </summary>
    [Fact]
    public async Task ProcesarPendientesAsync_ProcesaSoloFacturasActivas()
    {
        var primera = CrearFactura(EstadoRecordatorio.PrimerRecordatorio, "factura-1");
        var segunda = CrearFactura(EstadoRecordatorio.SegundoRecordatorio, "factura-2");
        var desactivada = CrearFactura(EstadoRecordatorio.Desactivado, "factura-3");
        var repositorio = new FacturaRepositoryFake(primera, segunda, desactivada);
        var correo = new EmailServiceFake();
        var servicio = new RecordatorioService(repositorio, correo);

        var resultados = (await servicio.ProcesarPendientesAsync()).ToList();

        Assert.Equal(2, resultados.Count);
        Assert.All(resultados, resultado => Assert.True(resultado.Exito));
        Assert.Equal(2, correo.EstadosEnviados.Count);
        Assert.Equal(2, repositorio.CantidadActualizaciones);
        Assert.Equal(EstadoRecordatorio.SegundoRecordatorio, primera.Estado);
        Assert.Equal(EstadoRecordatorio.Desactivado, segunda.Estado);
        Assert.Equal(EstadoRecordatorio.Desactivado, desactivada.Estado);
        Assert.Null(desactivada.UltimoCorreoEnviado);
    }

    private static Factura CrearFactura(
        EstadoRecordatorio estado,
        string id = "factura-prueba") => new()
    {
        Id = id,
        NumeroFactura = "SUS-001",
        Socio = new Socio
        {
            Id = "socio-prueba",
            Nombre = "Socio de prueba",
            Correo = "socio@example.com",
            PlanSuscripcion = "Premium"
        },
        Valor = 150_000m,
        FechaVencimiento = new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc),
        Estado = estado
    };

    private sealed class FacturaRepositoryFake(params Factura[] facturas) : IFacturaRepository
    {
        private readonly List<Factura> _facturas = [.. facturas];

        public int CantidadActualizaciones { get; private set; }

        public Task<IEnumerable<Factura>> ObtenerTodasAsync() =>
            Task.FromResult<IEnumerable<Factura>>(_facturas);

        public Task<Factura?> ObtenerPorIdAsync(string id) =>
            Task.FromResult(_facturas.SingleOrDefault(factura => factura.Id == id));

        public Task CrearAsync(Factura factura)
        {
            _facturas.Add(factura);
            return Task.CompletedTask;
        }

        public Task ActualizarAsync(Factura factura)
        {
            CantidadActualizaciones++;
            return Task.CompletedTask;
        }

        public Task<bool> EliminarAsync(string id)
        {
            var factura = _facturas.SingleOrDefault(item => item.Id == id);
            return Task.FromResult(factura is not null && _facturas.Remove(factura));
        }
    }

    private sealed class EmailServiceFake : IEmailService
    {
        public List<EstadoRecordatorio> EstadosEnviados { get; } = [];

        public Exception? Excepcion { get; init; }

        public Task EnviarRecordatorioAsync(Factura factura)
        {
            EstadosEnviados.Add(factura.Estado);

            return Excepcion is null
                ? Task.CompletedTask
                : Task.FromException(Excepcion);
        }
    }
}
