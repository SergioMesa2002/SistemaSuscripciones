using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SistemaSuscripciones.Infrastructure.Email;

/// <summary>
/// Conserva la configuración SMTP cifrada en el servidor para reutilizarla después de reiniciar la API.
/// </summary>
public sealed class SmtpSettingsStore
{
    private const string NombreArchivo = ".smtp-settings.enc";
    private readonly object _bloqueo = new();
    private readonly IDataProtector _protector;
    private readonly ILogger<SmtpSettingsStore> _logger;
    private readonly string _rutaArchivo;
    private SmtpSettings _settings;

    /// <summary>
    /// Inicializa el almacén y recupera el archivo cifrado cuando ya existe una configuración guardada.
    /// </summary>
    /// <param name="dataProtectionProvider">Proveedor que cifra el contenido antes de escribirlo en disco.</param>
    /// <param name="environment">Entorno usado para ubicar el archivo privado junto al proyecto de la API.</param>
    /// <param name="logger">Registro técnico que informa fallos sin incluir datos SMTP.</param>
    public SmtpSettingsStore(
        IDataProtectionProvider dataProtectionProvider,
        IHostEnvironment environment,
        ILogger<SmtpSettingsStore> logger)
    {
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);

        _protector = dataProtectionProvider.CreateProtector("SistemaSuscripciones.SmtpSettings.v1");
        _logger = logger;
        _rutaArchivo = Path.Combine(environment.ContentRootPath, NombreArchivo);
        _settings = Cargar() ?? CrearPredeterminada();
    }

    /// <summary>
    /// Obtiene una copia aislada de las opciones activas para evitar modificaciones accidentales.
    /// </summary>
    /// <returns>Una copia de la configuración SMTP vigente.</returns>
    public SmtpSettings Obtener()
    {
        lock (_bloqueo)
        {
            return Copiar(_settings);
        }
    }

    /// <summary>
    /// Cifra y persiste los valores antes de activarlos para los próximos envíos.
    /// </summary>
    /// <param name="settings">Valores SMTP validados que deben guardarse protegidos en el servidor.</param>
    public void Actualizar(SmtpSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_bloqueo)
        {
            var copia = Copiar(settings);
            var contenido = JsonSerializer.Serialize(copia);
            var contenidoProtegido = _protector.Protect(contenido);
            var rutaTemporal = $"{_rutaArchivo}.tmp";

            File.WriteAllText(rutaTemporal, contenidoProtegido);
            File.Move(rutaTemporal, _rutaArchivo, true);
            _settings = copia;
        }
    }

    private SmtpSettings? Cargar()
    {
        if (!File.Exists(_rutaArchivo))
        {
            return null;
        }

        try
        {
            var contenidoProtegido = File.ReadAllText(_rutaArchivo);
            var contenido = _protector.Unprotect(contenidoProtegido);
            return JsonSerializer.Deserialize<SmtpSettings>(contenido);
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or CryptographicException
            or JsonException)
        {
            _logger.LogWarning(
                "No fue posible recuperar la configuración SMTP cifrada. Tipo de error: {TipoError}.",
                exception.GetType().Name);
            return null;
        }
    }

    private static SmtpSettings CrearPredeterminada() => new()
    {
        Host = "smtp.gmail.com",
        Puerto = 587,
        HabilitarSsl = true
    };

    private static SmtpSettings Copiar(SmtpSettings settings) => new()
    {
        Host = settings.Host,
        Puerto = settings.Puerto,
        Usuario = settings.Usuario,
        Contrasena = settings.Contrasena,
        Remitente = settings.Remitente,
        HabilitarSsl = settings.HabilitarSsl,
        AsuntoPrimerRecordatorio = settings.AsuntoPrimerRecordatorio,
        MensajePrimerRecordatorio = settings.MensajePrimerRecordatorio,
        AsuntoSegundoRecordatorio = settings.AsuntoSegundoRecordatorio,
        MensajeSegundoRecordatorio = settings.MensajeSegundoRecordatorio
    };
}
