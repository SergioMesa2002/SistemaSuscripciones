using SistemaSuscripciones.Application.DTOs;

namespace SistemaSuscripciones.Application.Interfaces;

/// <summary>
/// Expone los casos de uso de administración de facturas sin trasladar persistencia a la capa HTTP.
/// </summary>
public interface IFacturaService
{
    /// <summary>
    /// Consulta de forma asíncrona los resúmenes disponibles para presentación.
    /// </summary>
    /// <returns>Una tarea que contiene los resúmenes de las facturas almacenadas.</returns>
    Task<IEnumerable<FacturaResumenDto>> ObtenerTodasAsync();

    /// <summary>
    /// Crea de forma asíncrona una factura y asigna sus identificadores internos.
    /// </summary>
    /// <param name="solicitud">Datos validados necesarios para registrar la factura.</param>
    /// <returns>Una tarea que contiene el resumen de la factura creada.</returns>
    Task<FacturaResumenDto> CrearAsync(GuardarFacturaDto solicitud);

    /// <summary>
    /// Actualiza de forma asíncrona una factura sin eliminar la auditoría del último envío.
    /// </summary>
    /// <param name="id">Identificador de la factura que se desea modificar.</param>
    /// <param name="solicitud">Nuevos valores editables de la factura.</param>
    /// <returns>Una tarea con el resumen actualizado, o <see langword="null"/> cuando no existe.</returns>
    Task<FacturaResumenDto?> ActualizarAsync(string id, GuardarFacturaDto solicitud);

    /// <summary>
    /// Elimina de forma asíncrona una factura mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador de la factura que se desea retirar.</param>
    /// <returns>Una tarea cuyo resultado indica si la factura existía y fue eliminada.</returns>
    Task<bool> EliminarAsync(string id);
}
