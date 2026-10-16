using System.Globalization;
using System.Net;
using System.Net.Mail;
using SistemaSuscripciones.Application.Interfaces;
using SistemaSuscripciones.Domain.Entities;
using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Infrastructure.Email;

/// <summary>
/// Entrega recordatorios de pago por SMTP con un mensaje acorde con la etapa de la factura.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettingsStore _settingsStore;

    /// <summary>
    /// Inicializa el servicio con opciones externas para evitar incorporar credenciales en el código.
    /// </summary>
    /// <param name="settingsStore">Almacén seguro que proporciona las opciones activas y conserva cifrados los secretos.</param>
    public SmtpEmailService(SmtpSettingsStore settingsStore)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);
        _settingsStore = settingsStore;
    }

    /// <summary>
    /// Envía de forma asíncrona el aviso correspondiente y rechaza facturas cuyo ciclo ya terminó.
    /// </summary>
    /// <param name="factura">Factura que determina el destinatario, el importe y el tipo de aviso.</param>
    /// <returns>Una tarea que representa la finalización del envío SMTP.</returns>
    /// <exception cref="InvalidOperationException">Se produce cuando la factura está desactivada y no admite más avisos.</exception>
    public async Task EnviarRecordatorioAsync(Factura factura)
    {
        ArgumentNullException.ThrowIfNull(factura);

        if (factura.Estado == EstadoRecordatorio.Desactivado)
        {
            throw new InvalidOperationException("No se puede enviar un recordatorio para una factura desactivada.");
        }

        var settings = _settingsStore.Obtener();
        var (asunto, cuerpo) = CrearContenido(factura, settings);

        using var mensaje = new MailMessage(
            settings.Remitente,
            factura.Socio.Correo,
            asunto,
            cuerpo);

        using var cliente = new SmtpClient(settings.Host, settings.Puerto)
        {
            EnableSsl = settings.HabilitarSsl,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(settings.Usuario))
        {
            cliente.Credentials = new NetworkCredential(settings.Usuario, settings.Contrasena);
        }

        await cliente.SendMailAsync(mensaje);
    }

    private static (string Asunto, string Cuerpo) CrearContenido(
        Factura factura,
        SmtpSettings settings)
    {
        var plantilla = factura.Estado switch
        {
            EstadoRecordatorio.PrimerRecordatorio =>
                (settings.AsuntoPrimerRecordatorio, settings.MensajePrimerRecordatorio),
            EstadoRecordatorio.SegundoRecordatorio =>
                (settings.AsuntoSegundoRecordatorio, settings.MensajeSegundoRecordatorio),
            _ => throw new InvalidOperationException("El estado de la factura no permite crear un recordatorio.")
        };

        return (
            AplicarVariables(plantilla.Item1, factura),
            AplicarVariables(plantilla.Item2, factura));
    }

    private static string AplicarVariables(string plantilla, Factura factura)
    {
        var cultura = CultureInfo.GetCultureInfo("es-CO");

        return plantilla
            .Replace("{NombreSocio}", factura.Socio.Nombre, StringComparison.Ordinal)
            .Replace("{CorreoSocio}", factura.Socio.Correo, StringComparison.Ordinal)
            .Replace("{PlanSuscripcion}", factura.Socio.PlanSuscripcion, StringComparison.Ordinal)
            .Replace("{NumeroFactura}", factura.NumeroFactura, StringComparison.Ordinal)
            .Replace("{Valor}", factura.Valor.ToString("N2", cultura), StringComparison.Ordinal)
            .Replace(
                "{FechaVencimiento}",
                factura.FechaVencimiento.ToString("dd/MM/yyyy", cultura),
                StringComparison.Ordinal);
    }
}
