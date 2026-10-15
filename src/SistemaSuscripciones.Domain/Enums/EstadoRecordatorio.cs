namespace SistemaSuscripciones.Domain.Enums;

/// <summary>
/// Representa la etapa del ciclo de avisos de pago de una factura.
/// </summary>
public enum EstadoRecordatorio
{
    /// <summary>
    /// Indica que la factura aún debe recibir su primer aviso de pago.
    /// </summary>
    PrimerRecordatorio,

    /// <summary>
    /// Indica que la factura ya recibió el primer aviso y requiere el aviso final.
    /// </summary>
    SegundoRecordatorio,

    /// <summary>
    /// Indica que el ciclo de avisos terminó y no deben enviarse más correos.
    /// </summary>
    Desactivado
}
