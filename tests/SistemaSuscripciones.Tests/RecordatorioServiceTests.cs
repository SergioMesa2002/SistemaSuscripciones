using System.Runtime.CompilerServices;
using NSubstitute;
using SistemaSuscripciones.Application.Interfaces;
using SistemaSuscripciones.Application.Services;
using SistemaSuscripciones.Domain.Entities;
using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Tests;

/// <summary>
/// Verifica con xUnit y NSubstitute las reglas del ciclo de recordatorios sin utilizar MongoDB ni SMTP.
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
        var repositorio = CrearRepositorioCon(factura);
        var correo = Substitute.For<IEmailService>();
        EstadoRecordatorio? estadoDuranteEnvio = null;
        correo.EnviarRecordatorioAsync(factura).Returns(_ =>
        {
            estadoDuranteEnvio = factura.Estado;
            return Task.CompletedTask;
        });
        var servicio = new RecordatorioService(repositorio, correo);
        var inicio = DateTime.UtcNow;

        var resultado = await servicio.EnviarAsync(factura.Id);

        Assert.True(resultado.Exito);
        Assert.Equal(EstadoRecordatorio.PrimerRecordatorio, estadoDuranteEnvio);
        Assert.Equal(EstadoRecordatorio.SegundoRecordatorio, resultado.NuevoEstado);
        Assert.Equal(EstadoRecordatorio.SegundoRecordatorio, factura.Estado);
        Assert.NotNull(factura.UltimoCorreoEnviado);
        Assert.InRange(factura.UltimoCorreoEnviado.Value, inicio, DateTime.UtcNow);
        await correo.Received(1).EnviarRecordatorioAsync(factura);
        await repositorio.Received(1).ActualizarAsync(factura);
    }

    /// <summary>
    /// Comprueba que el segundo aviso cierre el ciclo para impedir nuevos envíos.
    /// </summary>
    [Fact]
    public async Task EnviarAsync_SegundoRecordatorio_CambiaADesactivado()
    {
        var factura = CrearFactura(EstadoRecordatorio.SegundoRecordatorio);
        var repositorio = CrearRepositorioCon(factura);
        var correo = Substitute.For<IEmailService>();
        var servicio = new RecordatorioService(repositorio, correo);

        var resultado = await servicio.EnviarAsync(factura.Id);

        Assert.True(resultado.Exito);
        Assert.Equal(EstadoRecordatorio.Desactivado, resultado.NuevoEstado);
        Assert.Equal(EstadoRecordatorio.Desactivado, factura.Estado);
        await correo.Received(1).EnviarRecordatorioAsync(factura);
        await repositorio.Received(1).ActualizarAsync(factura);
    }

    /// <summary>
    /// Comprueba que una factura cerrada no invoque correo ni persistencia.
    /// </summary>
    [Fact]
    public async Task EnviarAsync_Desactivado_NoEnviaNiActualiza()
    {
        var factura = CrearFactura(EstadoRecordatorio.Desactivado);
        var repositorio = CrearRepositorioCon(factura);
        var correo = Substitute.For<IEmailService>();
        var servicio = new RecordatorioService(repositorio, correo);

        var resultado = await servicio.EnviarAsync(factura.Id);

        Assert.False(resultado.Exito);
        Assert.Equal(EstadoRecordatorio.Desactivado, resultado.NuevoEstado);
        Assert.Null(factura.UltimoCorreoEnviado);
        await correo.DidNotReceive().EnviarRecordatorioAsync(Arg.Any<Factura>());
        await repositorio.DidNotReceive().ActualizarAsync(Arg.Any<Factura>());
    }

    /// <summary>
    /// Comprueba que un fallo SMTP conserve intactos el estado y la auditoría de la factura.
    /// </summary>
    [Fact]
    public async Task EnviarAsync_CuandoCorreoFalla_NoCambiaEstadoNiPersiste()
    {
        var factura = CrearFactura(EstadoRecordatorio.PrimerRecordatorio);
        var repositorio = CrearRepositorioCon(factura);
        var correo = Substitute.For<IEmailService>();
        correo.EnviarRecordatorioAsync(factura)
            .Returns(Task.FromException(new InvalidOperationException("Fallo SMTP simulado.")));
        var servicio = new RecordatorioService(repositorio, correo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.EnviarAsync(factura.Id));

        Assert.Equal(EstadoRecordatorio.PrimerRecordatorio, factura.Estado);
        Assert.Null(factura.UltimoCorreoEnviado);
        await correo.Received(1).EnviarRecordatorioAsync(factura);
        await repositorio.DidNotReceive().ActualizarAsync(Arg.Any<Factura>());
    }

    /// <summary>
    /// Comprueba que un identificador desconocido produzca un resultado controlado sin efectos externos.
    /// </summary>
    [Fact]
    public async Task EnviarAsync_FacturaInexistente_DevuelveResultadoFallido()
    {
        var repositorio = Substitute.For<IFacturaRepository>();
        repositorio.ObtenerPorIdAsync("no-existe").Returns(Task.FromResult<Factura?>(null));
        var correo = Substitute.For<IEmailService>();
        var servicio = new RecordatorioService(repositorio, correo);

        var resultado = await servicio.EnviarAsync("no-existe");

        Assert.False(resultado.Exito);
        Assert.Equal("no-existe", resultado.FacturaId);
        Assert.Null(resultado.NuevoEstado);
        Assert.Contains("No se encontró", resultado.Mensaje);
        await correo.DidNotReceive().EnviarRecordatorioAsync(Arg.Any<Factura>());
        await repositorio.DidNotReceive().ActualizarAsync(Arg.Any<Factura>());
    }

    /// <summary>
    /// Comprueba que el procesamiento masivo consuma únicamente el flujo de facturas activas del repositorio.
    /// </summary>
    [Fact]
    public async Task ProcesarPendientesAsync_ProcesaSoloFacturasActivas()
    {
        var primera = CrearFactura(EstadoRecordatorio.PrimerRecordatorio, "factura-1");
        var segunda = CrearFactura(EstadoRecordatorio.SegundoRecordatorio, "factura-2");
        var desactivada = CrearFactura(EstadoRecordatorio.Desactivado, "factura-3");
        var repositorio = Substitute.For<IFacturaRepository>();
        repositorio.ObtenerPendientesAsync(Arg.Any<CancellationToken>())
            .Returns(CrearFlujoAsync(primera, segunda));
        var correo = Substitute.For<IEmailService>();
        var servicio = new RecordatorioService(repositorio, correo);

        var resultados = (await servicio.ProcesarPendientesAsync()).ToList();

        Assert.Equal(2, resultados.Count);
        Assert.All(resultados, resultado => Assert.True(resultado.Exito));
        Assert.Equal(EstadoRecordatorio.SegundoRecordatorio, primera.Estado);
        Assert.Equal(EstadoRecordatorio.Desactivado, segunda.Estado);
        Assert.Equal(EstadoRecordatorio.Desactivado, desactivada.Estado);
        Assert.Null(desactivada.UltimoCorreoEnviado);
        repositorio.Received(1).ObtenerPendientesAsync(Arg.Any<CancellationToken>());
        await repositorio.Received(2).ActualizarAsync(Arg.Any<Factura>());
        await repositorio.DidNotReceive().ObtenerTodasAsync();
        await correo.Received(2).EnviarRecordatorioAsync(Arg.Any<Factura>());
        await correo.DidNotReceive().EnviarRecordatorioAsync(desactivada);
    }

    private static IFacturaRepository CrearRepositorioCon(Factura factura)
    {
        var repositorio = Substitute.For<IFacturaRepository>();
        repositorio.ObtenerPorIdAsync(factura.Id)
            .Returns(Task.FromResult<Factura?>(factura));
        return repositorio;
    }

    private static Factura CrearFactura(
        EstadoRecordatorio estado,
        string id = "factura-prueba")
    {
        var socio = Socio.Crear(
            "socio-prueba",
            "Socio de prueba",
            "socio@example.com",
            "Premium");
        return Factura.Crear(
            id,
            "SUS-001",
            socio,
            150_000m,
            new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc),
            estado);
    }

    private static async IAsyncEnumerable<Factura> CrearFlujoAsync(
        Factura primera,
        Factura segunda,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return primera;
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return segunda;
    }
}
