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
}
