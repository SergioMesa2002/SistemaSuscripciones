using System.ComponentModel.DataAnnotations;

namespace SistemaSuscripciones.Api.Models;

/// <summary>
/// Contiene los valores SMTP que pueden guardarse de forma segura durante el desarrollo local.
/// </summary>
public sealed class ConfiguracionSmtpDto
{
    /// <summary>
    /// Obtiene o establece el servidor responsable de entregar los mensajes.
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el puerto habilitado por el proveedor SMTP.
    /// </summary>
    [Range(1, 65535)]
    public int Puerto { get; set; } = 587;

    /// <summary>
    /// Obtiene o establece la cuenta utilizada para autenticarse ante el proveedor.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Usuario { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la contraseña de aplicación; un valor vacío conserva la ya almacenada.
    /// </summary>
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la dirección que aparecerá como origen del mensaje.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Remitente { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece si la comunicación debe protegerse con TLS.
    /// </summary>
    public bool HabilitarSsl { get; set; } = true;

    /// <summary>
    /// Obtiene o establece el asunto configurable para el primer aviso.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string AsuntoPrimerRecordatorio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el contenido configurable para el primer aviso.
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string MensajePrimerRecordatorio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el asunto configurable para el aviso final.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string AsuntoSegundoRecordatorio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el contenido configurable para el aviso final.
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string MensajeSegundoRecordatorio { get; set; } = string.Empty;
}

/// <summary>
/// Expone el estado no sensible de SMTP sin devolver la contraseña al cliente.
/// </summary>
public sealed class EstadoConfiguracionSmtpDto
{
    /// <summary>
    /// Obtiene o establece el servidor SMTP activo.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el puerto SMTP activo.
    /// </summary>
    public int Puerto { get; set; }

    /// <summary>
    /// Obtiene o establece el usuario configurado para autenticar los envíos.
    /// </summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el remitente visible configurado.
    /// </summary>
    public string Remitente { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece si TLS está habilitado.
    /// </summary>
    public bool HabilitarSsl { get; set; }

    /// <summary>
    /// Obtiene o establece si existe una contraseña cifrada sin revelar su contenido.
    /// </summary>
    public bool ContrasenaConfigurada { get; set; }

    /// <summary>
    /// Obtiene o establece si todos los valores necesarios están disponibles.
    /// </summary>
    public bool Configurada { get; set; }

    /// <summary>
    /// Obtiene o establece el asunto activo para el primer aviso.
    /// </summary>
    public string AsuntoPrimerRecordatorio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la plantilla activa para el primer aviso.
    /// </summary>
    public string MensajePrimerRecordatorio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el asunto activo para el aviso final.
    /// </summary>
    public string AsuntoSegundoRecordatorio { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la plantilla activa para el aviso final.
    /// </summary>
    public string MensajeSegundoRecordatorio { get; set; } = string.Empty;
}

/// <summary>
/// Identifica el destinatario que recibirá el mensaje utilizado para validar SMTP.
/// </summary>
public sealed class ProbarSmtpDto
{
    /// <summary>
    /// Obtiene o establece el correo donde se comprobará la entrega.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Destinatario { get; set; } = string.Empty;
}

/// <summary>
/// Comunica de forma segura el resultado de una prueba de entrega SMTP.
/// </summary>
public sealed class ResultadoPruebaSmtpDto
{
    /// <summary>
    /// Obtiene o establece si el proveedor aceptó el mensaje de prueba.
    /// </summary>
    public bool Exito { get; set; }

    /// <summary>
    /// Obtiene o establece una explicación comprensible sin detalles sensibles.
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;
}
