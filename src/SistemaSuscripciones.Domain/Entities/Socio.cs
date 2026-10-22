using System.Net.Mail;

namespace SistemaSuscripciones.Domain.Entities;

/// <summary>
/// Representa al titular de una suscripción y protege la consistencia de sus datos de contacto.
/// </summary>
public sealed class Socio
{
    private Socio()
    {
    }

    /// <summary>
    /// Obtiene el identificador único e inmutable del socio.
    /// </summary>
    public string Id { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene el nombre utilizado para identificar y contactar al socio.
    /// </summary>
    public string Nombre { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene la dirección validada a la que se enviarán los avisos de pago.
    /// </summary>
    public string Correo { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene el plan contratado que da contexto a la suscripción.
    /// </summary>
    public string PlanSuscripcion { get; private set; } = string.Empty;

    /// <summary>
    /// Crea un socio garantizando que los datos necesarios para identificarlo y contactarlo sean válidos.
    /// </summary>
    /// <param name="id">Identificador único asignado al socio.</param>
    /// <param name="nombre">Nombre que se mostrará en facturas y comunicaciones.</param>
    /// <param name="correo">Dirección válida a la que se enviarán los recordatorios.</param>
    /// <param name="planSuscripcion">Plan contratado por el socio.</param>
    /// <returns>Un socio consistente y listo para asociarse con una factura.</returns>
    public static Socio Crear(string id, string nombre, string correo, string planSuscripcion)
    {
        var socio = new Socio
        {
            Id = ValidarTextoObligatorio(id, nameof(id))
        };

        socio.ActualizarDatos(nombre, correo, planSuscripcion);
        return socio;
    }

    /// <summary>
    /// Modifica los datos comerciales del socio sin permitir que quede en un estado inconsistente.
    /// </summary>
    /// <param name="nombre">Nombre que se mostrará en facturas y comunicaciones.</param>
    /// <param name="correo">Dirección válida a la que se enviarán los recordatorios.</param>
    /// <param name="planSuscripcion">Plan contratado por el socio.</param>
    public void ActualizarDatos(string nombre, string correo, string planSuscripcion)
    {
        var correoNormalizado = ValidarTextoObligatorio(correo, nameof(correo));
        if (!MailAddress.TryCreate(correoNormalizado, out var direccion)
            || !string.Equals(direccion.Address, correoNormalizado, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("El correo del socio no tiene un formato válido.", nameof(correo));
        }

        Nombre = ValidarTextoObligatorio(nombre, nameof(nombre));
        Correo = direccion.Address;
        PlanSuscripcion = ValidarTextoObligatorio(planSuscripcion, nameof(planSuscripcion));
    }

    private static string ValidarTextoObligatorio(string valor, string nombreParametro)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException("El valor es obligatorio.", nombreParametro);
        }

        return valor.Trim();
    }
}
