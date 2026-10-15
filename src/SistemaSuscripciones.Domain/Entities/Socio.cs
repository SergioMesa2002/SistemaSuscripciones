namespace SistemaSuscripciones.Domain.Entities;

/// <summary>
/// Representa al titular de una suscripción asociado con sus facturas.
/// </summary>
public class Socio
{
    /// <summary>
    /// Obtiene o establece el identificador único del socio.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el nombre utilizado para identificar y contactar al socio.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la dirección a la que se enviarán los avisos de pago.
    /// </summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el plan contratado que da contexto a la suscripción.
    /// </summary>
    public string PlanSuscripcion { get; set; } = string.Empty;
}
