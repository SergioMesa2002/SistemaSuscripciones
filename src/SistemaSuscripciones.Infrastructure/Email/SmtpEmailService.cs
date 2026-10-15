using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SistemaSuscripciones.Application.Interfaces;
using SistemaSuscripciones.Domain.Entities;
using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Infrastructure.Email;

/// <summary>
/// Entrega recordatorios de pago por SMTP con un mensaje acorde con la etapa de la factura.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;

    /// <summary>
    /// Inicializa el servicio con opciones externas para evitar incorporar credenciales en el código.
    /// </summary>
    /// <param name="options">Opciones de conexión y autenticación del servidor SMTP.</param>
    public SmtpEmailService(IOptions<SmtpSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _settings = options.Value;
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

        var (asunto, cuerpo) = CrearContenido(factura);

        using var mensaje = new MailMessage(
            _settings.Remitente,
            factura.Socio.Correo,
            asunto,
            cuerpo);

        using var cliente = new SmtpClient(_settings.Host, _settings.Puerto)
        {
            EnableSsl = _settings.HabilitarSsl,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_settings.Usuario))
        {
            cliente.Credentials = new NetworkCredential(_settings.Usuario, _settings.Contrasena);
        }

        await cliente.SendMailAsync(mensaje);
    }

    private static (string Asunto, string Cuerpo) CrearContenido(Factura factura)
    {
        return factura.Estado switch
        {
            EstadoRecordatorio.PrimerRecordatorio =>
                (
                    $"Recordatorio de pago - Factura {factura.NumeroFactura}",
                    $"Hola {factura.Socio.Nombre},\n\n" +
                    $"Este es un primer recordatorio del pago pendiente por {factura.Valor:N2}, " +
                    $"correspondiente a la factura {factura.NumeroFactura}, con vencimiento " +
                    $"el {factura.FechaVencimiento:dd/MM/yyyy}.\n\n" +
                    "Por favor, realiza el pago a la mayor brevedad posible."
                ),
            EstadoRecordatorio.SegundoRecordatorio =>
                (
                    $"Último recordatorio de pago - Factura {factura.NumeroFactura}",
                    $"Hola {factura.Socio.Nombre},\n\n" +
                    $"Este es el último aviso del pago pendiente por {factura.Valor:N2}, " +
                    $"correspondiente a la factura {factura.NumeroFactura}.\n\n" +
                    "Si el pago no se regulariza, los recordatorios de esta factura serán desactivados."
                ),
            _ => throw new InvalidOperationException("El estado de la factura no permite crear un recordatorio.")
        };
    }
}
