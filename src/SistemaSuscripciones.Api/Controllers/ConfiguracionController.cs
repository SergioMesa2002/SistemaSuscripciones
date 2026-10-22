using Microsoft.AspNetCore.Mvc;
using SistemaSuscripciones.Api.Models;
using SistemaSuscripciones.Application.Interfaces;
using SistemaSuscripciones.Domain.Entities;
using SistemaSuscripciones.Infrastructure.Email;

namespace SistemaSuscripciones.Api.Controllers;

/// <summary>
/// Permite guardar y probar una configuración SMTP exclusivamente durante el desarrollo local.
/// </summary>
[ApiController]
[Route("api/configuracion/smtp")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ConfiguracionController : ControllerBase
{
    private readonly SmtpSettingsStore _settingsStore;
    private readonly IEmailService _emailService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ConfiguracionController> _logger;

    /// <summary>
    /// Inicializa el controlador con el almacén cifrado, el servicio de envío y el entorno actual.
    /// </summary>
    /// <param name="settingsStore">Almacén persistente que cifra la contraseña antes de escribirla.</param>
    /// <param name="emailService">Servicio utilizado para comprobar la entrega real.</param>
    /// <param name="environment">Entorno que restringe estos endpoints a desarrollo.</param>
    /// <param name="logger">Registro técnico que evita escribir credenciales.</param>
    public ConfiguracionController(
        SmtpSettingsStore settingsStore,
        IEmailService emailService,
        IWebHostEnvironment environment,
        ILogger<ConfiguracionController> logger)
    {
        _settingsStore = settingsStore;
        _emailService = emailService;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Consulta los valores SMTP no sensibles guardados en el servidor local.
    /// </summary>
    /// <returns>Una respuesta asíncrona con el estado SMTP sin incluir la contraseña.</returns>
    /// <response code="200">Devuelve la configuración visible y si existe un secreto cargado.</response>
    /// <response code="404">Indica que la operación no está disponible fuera de desarrollo.</response>
    [HttpGet]
    [ProducesResponseType(typeof(EstadoConfiguracionSmtpDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<EstadoConfiguracionSmtpDto> Obtener()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(CrearEstado(_settingsStore.Obtener()));
    }

    /// <summary>
    /// Guarda valores SMTP cifrados para reutilizarlos en los próximos inicios de la API.
    /// </summary>
    /// <param name="solicitud">Valores de conexión; una contraseña vacía conserva la que ya está almacenada.</param>
    /// <returns>Una respuesta asíncrona con la configuración no sensible resultante.</returns>
    /// <response code="200">Indica que los próximos correos utilizarán los nuevos valores.</response>
    /// <response code="400">Indica que faltan datos o no existe una contraseña disponible.</response>
    /// <response code="404">Indica que la operación no está disponible fuera de desarrollo.</response>
    [HttpPut]
    [ProducesResponseType(typeof(EstadoConfiguracionSmtpDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<EstadoConfiguracionSmtpDto> Actualizar(ConfiguracionSmtpDto solicitud)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var actual = _settingsStore.Obtener();
        var contrasena = string.IsNullOrWhiteSpace(solicitud.Contrasena)
            ? actual.Contrasena
            : solicitud.Contrasena;

        if (string.IsNullOrWhiteSpace(contrasena))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Falta la contraseña SMTP.",
                Detail = "Ingresa una contraseña de aplicación para habilitar los envíos.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var settings = new SmtpSettings
        {
            Host = solicitud.Host.Trim(),
            Puerto = solicitud.Puerto,
            Usuario = solicitud.Usuario.Trim(),
            Contrasena = contrasena,
            Remitente = solicitud.Remitente.Trim(),
            HabilitarSsl = solicitud.HabilitarSsl,
            AsuntoPrimerRecordatorio = solicitud.AsuntoPrimerRecordatorio.Trim(),
            MensajePrimerRecordatorio = solicitud.MensajePrimerRecordatorio.Trim(),
            AsuntoSegundoRecordatorio = solicitud.AsuntoSegundoRecordatorio.Trim(),
            MensajeSegundoRecordatorio = solicitud.MensajeSegundoRecordatorio.Trim()
        };

        _settingsStore.Actualizar(settings);
        return Ok(CrearEstado(settings));
    }

    /// <summary>
    /// Envía un recordatorio de prueba para verificar las credenciales y la conectividad SMTP.
    /// </summary>
    /// <param name="solicitud">Correo que debe recibir el mensaje de comprobación.</param>
    /// <returns>Una respuesta asíncrona que indica si el proveedor aceptó el correo.</returns>
    /// <response code="200">Indica que el mensaje fue aceptado para entrega.</response>
    /// <response code="400">Indica que la configuración todavía está incompleta.</response>
    /// <response code="404">Indica que la operación no está disponible fuera de desarrollo.</response>
    /// <response code="502">Indica que el proveedor SMTP rechazó o no pudo procesar la conexión.</response>
    [HttpPost("probar")]
    [ProducesResponseType(typeof(ResultadoPruebaSmtpDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ResultadoPruebaSmtpDto>> ProbarAsync(ProbarSmtpDto solicitud)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var settings = _settingsStore.Obtener();
        if (!EstaConfigurada(settings))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Configuración SMTP incompleta.",
                Detail = "Guarda el servidor, puerto, usuario, contraseña y remitente antes de probar.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var socioPrueba = Socio.Crear(
            Guid.NewGuid().ToString("N"),
            "Prueba SMTP",
            solicitud.Destinatario,
            "Configuración local");
        var facturaPrueba = Factura.Crear(
            Guid.NewGuid().ToString("N"),
            "SMTP-PRUEBA",
            socioPrueba,
            1m,
            DateTime.UtcNow);

        try
        {
            await _emailService.EnviarRecordatorioAsync(facturaPrueba);
            return Ok(new ResultadoPruebaSmtpDto
            {
                Exito = true,
                Mensaje = $"El proveedor aceptó el correo de prueba para {solicitud.Destinatario}."
            });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "La prueba SMTP falló. Tipo de error: {TipoError}.",
                exception.GetType().Name);

            return Problem(
                title: "No fue posible enviar el correo de prueba.",
                detail: "Revisa el servidor, el puerto, el correo y la contraseña de aplicación.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static EstadoConfiguracionSmtpDto CrearEstado(SmtpSettings settings) => new()
    {
        Host = settings.Host,
        Puerto = settings.Puerto,
        Usuario = settings.Usuario,
        Remitente = settings.Remitente,
        HabilitarSsl = settings.HabilitarSsl,
        ContrasenaConfigurada = !string.IsNullOrWhiteSpace(settings.Contrasena),
        Configurada = EstaConfigurada(settings),
        AsuntoPrimerRecordatorio = settings.AsuntoPrimerRecordatorio,
        MensajePrimerRecordatorio = settings.MensajePrimerRecordatorio,
        AsuntoSegundoRecordatorio = settings.AsuntoSegundoRecordatorio,
        MensajeSegundoRecordatorio = settings.MensajeSegundoRecordatorio
    };

    private static bool EstaConfigurada(SmtpSettings settings) =>
        !string.IsNullOrWhiteSpace(settings.Host)
        && settings.Puerto is > 0 and <= 65535
        && !string.IsNullOrWhiteSpace(settings.Usuario)
        && !string.IsNullOrWhiteSpace(settings.Contrasena)
        && !string.IsNullOrWhiteSpace(settings.Remitente);
}
