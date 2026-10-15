using System.ComponentModel.DataAnnotations;
using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Application.DTOs;

/// <summary>
/// Contiene los datos editables necesarios para crear o actualizar una factura desde la API.
/// </summary>
public class GuardarFacturaDto
{
    /// <summary>
    /// Obtiene o establece el consecutivo visible que identifica la factura ante el socio.
    /// </summary>
    [Required]
    public string NumeroFactura { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el nombre del socio responsable del pago.
    /// </summary>
    [Required]
    public string NombreSocio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la dirección válida donde se entregarán los recordatorios.
    /// </summary>
    [Required]
    [EmailAddress]
    public string CorreoSocio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el plan contratado al que corresponde la obligación.
    /// </summary>
    [Required]
    public string PlanSuscripcion { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el importe pendiente, que debe ser mayor que cero.
    /// </summary>
    [Range(0.01, 999999999999.99)]
    public decimal Valor { get; set; }

    /// <summary>
    /// Obtiene o establece la fecha límite utilizada para informar el vencimiento.
    /// </summary>
    public DateTime FechaVencimiento { get; set; }

    /// <summary>
    /// Obtiene o establece la etapa desde la que continuará el ciclo de recordatorios.
    /// </summary>
    [EnumDataType(typeof(EstadoRecordatorio))]
    public EstadoRecordatorio Estado { get; set; } = EstadoRecordatorio.PrimerRecordatorio;
}
