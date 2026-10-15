using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Domain.Entities;

/// <summary>
/// Representa una obligación de pago y el avance de sus recordatorios.
/// </summary>
public class Factura
{
    /// <summary>
    /// Obtiene o establece el identificador único usado para localizar la factura.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el consecutivo visible para el socio y los operadores.
    /// </summary>
    public string NumeroFactura { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el socio responsable de la obligación de pago.
    /// </summary>
    public Socio Socio { get; set; } = new();

    /// <summary>
    /// Obtiene o establece el importe pendiente asociado con la factura.
    /// </summary>
    public decimal Valor { get; set; }

    /// <summary>
    /// Obtiene o establece la fecha límite que determina la oportunidad del aviso.
    /// </summary>
    public DateTime FechaVencimiento { get; set; }

    /// <summary>
    /// Obtiene o establece la etapa actual del ciclo de recordatorios.
    /// </summary>
    public EstadoRecordatorio Estado { get; set; } = EstadoRecordatorio.PrimerRecordatorio;

    /// <summary>
    /// Obtiene o establece el instante UTC del último correo enviado, si existe.
    /// </summary>
    public DateTime? UltimoCorreoEnviado { get; set; }
}
