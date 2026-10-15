using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Application.DTOs;

/// <summary>
/// Proporciona los datos de una factura que necesitará la futura tabla de consulta.
/// </summary>
public class FacturaResumenDto
{
    /// <summary>
    /// Obtiene o establece el identificador con el que se podrán solicitar acciones sobre la factura.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el consecutivo que se mostrará al usuario.
    /// </summary>
    public string NumeroFactura { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el nombre del responsable del pago.
    /// </summary>
    public string NombreSocio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el correo de contacto del responsable del pago.
    /// </summary>
    public string CorreoSocio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el plan contratado al que corresponde la factura.
    /// </summary>
    public string PlanSuscripcion { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el importe pendiente que se presentará en el resumen.
    /// </summary>
    public decimal Valor { get; set; }

    /// <summary>
    /// Obtiene o establece la fecha límite de pago que se mostrará al usuario.
    /// </summary>
    public DateTime FechaVencimiento { get; set; }

    /// <summary>
    /// Obtiene o establece la etapa vigente del ciclo de recordatorios.
    /// </summary>
    public EstadoRecordatorio Estado { get; set; }

    /// <summary>
    /// Obtiene o establece el instante UTC del último aviso enviado, si existe.
    /// </summary>
    public DateTime? UltimoCorreoEnviado { get; set; }
}
