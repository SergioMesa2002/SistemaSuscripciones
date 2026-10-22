using SistemaSuscripciones.Application.DTOs;
using SistemaSuscripciones.Application.Interfaces;
using SistemaSuscripciones.Domain.Entities;

namespace SistemaSuscripciones.Application.Services;

/// <summary>
/// Coordina los casos de uso de consulta y mantenimiento de facturas.
/// </summary>
public sealed class FacturaService : IFacturaService
{
    private readonly IFacturaRepository _facturaRepository;

    /// <summary>
    /// Inicializa el servicio con la abstracción responsable de la persistencia.
    /// </summary>
    /// <param name="facturaRepository">Repositorio utilizado para almacenar y recuperar facturas.</param>
    public FacturaService(IFacturaRepository facturaRepository)
    {
        _facturaRepository = facturaRepository
            ?? throw new ArgumentNullException(nameof(facturaRepository));
    }

    /// <summary>
    /// Consulta de forma asíncrona las facturas y las transforma en modelos de salida.
    /// </summary>
    /// <returns>Una tarea que contiene los resúmenes disponibles.</returns>
    public async Task<IEnumerable<FacturaResumenDto>> ObtenerTodasAsync()
    {
        var facturas = await _facturaRepository.ObtenerTodasAsync();
        return facturas.Select(CrearResumen).ToList();
    }

    /// <summary>
    /// Construye y persiste de forma asíncrona una factura con identificadores nuevos.
    /// </summary>
    /// <param name="solicitud">Datos validados necesarios para registrar la factura.</param>
    /// <returns>Una tarea que contiene el resumen creado.</returns>
    public async Task<FacturaResumenDto> CrearAsync(GuardarFacturaDto solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var socio = Socio.Crear(
            Guid.NewGuid().ToString("N"),
            solicitud.NombreSocio,
            solicitud.CorreoSocio,
            solicitud.PlanSuscripcion);
        var factura = Factura.Crear(
            Guid.NewGuid().ToString("N"),
            solicitud.NumeroFactura,
            socio,
            solicitud.Valor,
            solicitud.FechaVencimiento,
            solicitud.Estado);

        await _facturaRepository.CrearAsync(factura);
        return CrearResumen(factura);
    }

    /// <summary>
    /// Aplica y persiste de forma asíncrona los datos editables de una factura existente.
    /// </summary>
    /// <param name="id">Identificador de la factura que se desea modificar.</param>
    /// <param name="solicitud">Nuevos valores editables de la factura.</param>
    /// <returns>Una tarea con el resumen actualizado, o <see langword="null"/> cuando no existe.</returns>
    public async Task<FacturaResumenDto?> ActualizarAsync(
        string id,
        GuardarFacturaDto solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var factura = await _facturaRepository.ObtenerPorIdAsync(id);
        if (factura is null)
        {
            return null;
        }

        factura.ActualizarDatosComerciales(
            solicitud.NumeroFactura,
            solicitud.Valor,
            solicitud.FechaVencimiento);
        factura.Socio.ActualizarDatos(
            solicitud.NombreSocio,
            solicitud.CorreoSocio,
            solicitud.PlanSuscripcion);
        factura.CambiarEstadoAdministrativamente(solicitud.Estado);

        await _facturaRepository.ActualizarAsync(factura);
        return CrearResumen(factura);
    }

    /// <summary>
    /// Solicita de forma asíncrona la eliminación de una factura.
    /// </summary>
    /// <param name="id">Identificador de la factura que se desea retirar.</param>
    /// <returns>Una tarea cuyo resultado indica si se eliminó la factura.</returns>
    public Task<bool> EliminarAsync(string id) =>
        _facturaRepository.EliminarAsync(id);

    private static FacturaResumenDto CrearResumen(Factura factura) => new()
    {
        Id = factura.Id,
        NumeroFactura = factura.NumeroFactura,
        NombreSocio = factura.Socio.Nombre,
        CorreoSocio = factura.Socio.Correo,
        PlanSuscripcion = factura.Socio.PlanSuscripcion,
        Valor = factura.Valor,
        FechaVencimiento = factura.FechaVencimiento,
        Estado = factura.Estado,
        UltimoCorreoEnviado = factura.UltimoCorreoEnviado
    };
}
