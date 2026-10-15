using SistemaSuscripciones.Domain.Entities;

namespace SistemaSuscripciones.Application.Interfaces;

/// <summary>
/// Abstrae el envío de avisos para mantener la lógica de aplicación independiente del proveedor de correo.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía de forma asíncrona el recordatorio correspondiente al estado actual de una factura.
    /// </summary>
    /// <param name="factura">Factura que aporta el destinatario y el contexto del aviso.</param>
    /// <returns>Una tarea que representa la finalización del envío.</returns>
    Task EnviarRecordatorioAsync(Factura factura);
}
