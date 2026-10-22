using System.Runtime.CompilerServices;
using SistemaSuscripciones.Application.DTOs;
using SistemaSuscripciones.Application.Interfaces;
using SistemaSuscripciones.Application.Services;
using SistemaSuscripciones.Domain.Entities;

namespace SistemaSuscripciones.Tests;

/// <summary>
/// Verifica que los casos de uso CRUD permanezcan fuera del controlador HTTP.
/// </summary>
public sealed class FacturaServiceTests
{
    /// <summary>
    /// Comprueba que la creación asigne identificadores y entregue la entidad al repositorio.
    /// </summary>
    [Fact]
    public async Task CrearAsync_DatosValidos_GeneraIdentificadoresYPersiste()
    {
        var repositorio = new FacturaRepositoryFake();
        var servicio = new FacturaService(repositorio);
        var solicitud = CrearSolicitud();

        var resultado = await servicio.CrearAsync(solicitud);

        var factura = Assert.Single(repositorio.Facturas);
        Assert.False(string.IsNullOrWhiteSpace(factura.Id));
        Assert.False(string.IsNullOrWhiteSpace(factura.Socio.Id));
        Assert.Equal(solicitud.NumeroFactura, resultado.NumeroFactura);
        Assert.Equal(1, repositorio.CantidadCreaciones);
    }

    /// <summary>
    /// Comprueba que la actualización de datos comerciales conserve la auditoría del último correo.
    /// </summary>
    [Fact]
    public async Task ActualizarAsync_FacturaExistente_ConservaUltimoCorreoEnviado()
    {
        var ultimoEnvio = new DateTime(2026, 10, 22, 14, 0, 0, DateTimeKind.Utc);
        var socio = Socio.Crear(
            "socio-1",
            "Socio anterior",
            "anterior@example.com",
            "Básico");
        var factura = Factura.Crear(
            "factura-1",
            "ANTERIOR",
            socio,
            100_000m,
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));
        factura.RegistrarRecordatorioEnviado(ultimoEnvio);
        var repositorio = new FacturaRepositoryFake(factura);
        var servicio = new FacturaService(repositorio);

        var resultado = await servicio.ActualizarAsync(factura.Id, CrearSolicitud());

        Assert.NotNull(resultado);
        Assert.Equal(ultimoEnvio, resultado.UltimoCorreoEnviado);
        Assert.Equal(1, repositorio.CantidadActualizaciones);
    }

    private static GuardarFacturaDto CrearSolicitud() => new()
    {
        NumeroFactura = "SUS-200",
        NombreSocio = "Socio de prueba",
        CorreoSocio = "socio@example.com",
        PlanSuscripcion = "Premium",
        Valor = 185_000m,
        FechaVencimiento = new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc)
    };

    private sealed class FacturaRepositoryFake(params Factura[] facturas) : IFacturaRepository
    {
        public List<Factura> Facturas { get; } = [.. facturas];

        public int CantidadCreaciones { get; private set; }

        public int CantidadActualizaciones { get; private set; }

        public Task<IEnumerable<Factura>> ObtenerTodasAsync() =>
            Task.FromResult<IEnumerable<Factura>>(Facturas);

        public async IAsyncEnumerable<Factura> ObtenerPendientesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var factura in Facturas)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return factura;
            }

            await Task.CompletedTask;
        }

        public Task<Factura?> ObtenerPorIdAsync(string id) =>
            Task.FromResult(Facturas.SingleOrDefault(factura => factura.Id == id));

        public Task CrearAsync(Factura factura)
        {
            CantidadCreaciones++;
            Facturas.Add(factura);
            return Task.CompletedTask;
        }

        public Task ActualizarAsync(Factura factura)
        {
            CantidadActualizaciones++;
            return Task.CompletedTask;
        }

        public Task<bool> EliminarAsync(string id)
        {
            var factura = Facturas.SingleOrDefault(item => item.Id == id);
            return Task.FromResult(factura is not null && Facturas.Remove(factura));
        }
    }
}
