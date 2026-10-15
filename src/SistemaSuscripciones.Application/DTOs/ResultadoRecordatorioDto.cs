using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Application.DTOs;

/// <summary>
/// Describe el resultado funcional de intentar procesar el recordatorio de una factura.
/// </summary>
public class ResultadoRecordatorioDto
{
    /// <summary>
    /// Obtiene o establece si el correo y la actualización se completaron correctamente.
    /// </summary>
    public bool Exito { get; set; }

    /// <summary>
    /// Obtiene o establece una explicación comprensible del resultado de la operación.
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el identificador de la factura solicitada para facilitar su seguimiento.
    /// </summary>
    public string FacturaId { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el estado resultante, o ningún valor cuando la factura no existe.
    /// </summary>
    public EstadoRecordatorio? NuevoEstado { get; set; }
}
