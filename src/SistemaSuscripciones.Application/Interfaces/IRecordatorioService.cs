using SistemaSuscripciones.Application.DTOs;

namespace SistemaSuscripciones.Application.Interfaces;

/// <summary>
/// Expone los casos de uso que coordinan el envío y avance de los recordatorios de pago.
/// </summary>
public interface IRecordatorioService
{
    /// <summary>
    /// Procesa de forma asíncrona el siguiente recordatorio de una factura específica.
    /// </summary>
    /// <param name="id">Identificador de la factura que se desea procesar.</param>
    /// <returns>Una tarea que contiene el resultado funcional del intento de envío.</returns>
    Task<ResultadoRecordatorioDto> EnviarAsync(string id);

    /// <summary>
    /// Procesa de forma asíncrona todas las facturas que todavía admiten recordatorios.
    /// </summary>
    /// <returns>Una tarea que contiene un resultado por cada factura activa procesada.</returns>
    Task<IEnumerable<ResultadoRecordatorioDto>> ProcesarPendientesAsync();
}
