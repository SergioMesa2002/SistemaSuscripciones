namespace SistemaSuscripciones.Infrastructure.Email;

/// <summary>
/// Reúne los valores de conexión y autenticación requeridos por el servidor de correo.
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// Identifica la sección de configuración desde la que se enlazan las opciones SMTP.
    /// </summary>
    public const string Seccion = "Smtp";

    /// <summary>
    /// Obtiene o establece el nombre o dirección del servidor que entregará los mensajes.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el puerto de conexión definido por el proveedor de correo.
    /// </summary>
    public int Puerto { get; set; } = 587;

    /// <summary>
    /// Obtiene o establece el usuario suministrado externamente para autenticar el envío.
    /// </summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el secreto suministrado externamente para autenticar el envío.
    /// </summary>
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la dirección que aparecerá como origen de los recordatorios.
    /// </summary>
    public string Remitente { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece si la conexión debe protegerse mediante SSL/TLS.
    /// </summary>
    public bool HabilitarSsl { get; set; } = true;

    /// <summary>
    /// Obtiene o establece el asunto utilizado cuando se envía el primer aviso de pago.
    /// </summary>
    public string AsuntoPrimerRecordatorio { get; set; } =
        "Recordatorio de pago - Factura {NumeroFactura}";

    /// <summary>
    /// Obtiene o establece la plantilla del mensaje enviado durante el primer aviso.
    /// </summary>
    public string MensajePrimerRecordatorio { get; set; } =
        "Hola {NombreSocio},\n\n" +
        "Este es un primer recordatorio del pago pendiente por {Valor}, " +
        "correspondiente a la factura {NumeroFactura}, con vencimiento el {FechaVencimiento}.\n\n" +
        "Por favor, realiza el pago a la mayor brevedad posible.";

    /// <summary>
    /// Obtiene o establece el asunto utilizado cuando se envía el último aviso de pago.
    /// </summary>
    public string AsuntoSegundoRecordatorio { get; set; } =
        "Último recordatorio de pago - Factura {NumeroFactura}";

    /// <summary>
    /// Obtiene o establece la plantilla del mensaje enviado antes de desactivar los avisos.
    /// </summary>
    public string MensajeSegundoRecordatorio { get; set; } =
        "Hola {NombreSocio},\n\n" +
        "Este es el último aviso del pago pendiente por {Valor}, " +
        "correspondiente a la factura {NumeroFactura}.\n\n" +
        "Si el pago no se regulariza, los recordatorios de esta factura serán desactivados.";
}
