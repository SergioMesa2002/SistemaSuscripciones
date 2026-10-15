using SistemaSuscripciones.Domain.Entities;

namespace SistemaSuscripciones.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia requeridas por los casos de uso de facturas sin acoplarlos a una base de datos.
/// </summary>
public interface IFacturaRepository
{
    /// <summary>
    /// Obtiene de forma asíncrona todas las facturas disponibles para su consulta o procesamiento.
    /// </summary>
    /// <returns>Una tarea que contiene la secuencia de facturas almacenadas.</returns>
    Task<IEnumerable<Factura>> ObtenerTodasAsync();

    /// <summary>
    /// Busca de forma asíncrona una factura mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador único de la factura que se desea recuperar.</param>
    /// <returns>Una tarea que contiene la factura encontrada, o <see langword="null"/> cuando no existe.</returns>
    Task<Factura?> ObtenerPorIdAsync(string id);

    /// <summary>
    /// Inserta de forma asíncrona una nueva factura para que pueda consultarse y procesarse posteriormente.
    /// </summary>
    /// <param name="factura">Nueva factura que debe incorporarse al almacenamiento.</param>
    /// <returns>Una tarea que representa la finalización de la inserción.</returns>
    Task CrearAsync(Factura factura);

    /// <summary>
    /// Persiste de forma asíncrona los cambios de una factura después de completar la operación de negocio.
    /// </summary>
    /// <param name="factura">Factura cuyos datos actualizados deben conservarse.</param>
    /// <returns>Una tarea que representa la finalización de la actualización.</returns>
    Task ActualizarAsync(Factura factura);

    /// <summary>
    /// Elimina de forma asíncrona la factura identificada, si todavía existe.
    /// </summary>
    /// <param name="id">Identificador único de la factura que se desea eliminar.</param>
    /// <returns>Una tarea cuyo resultado indica si se eliminó algún documento.</returns>
    Task<bool> EliminarAsync(string id);
}
