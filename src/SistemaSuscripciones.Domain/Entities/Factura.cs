using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Domain.Entities;

/// <summary>
/// Representa una obligación de pago y protege las reglas de su ciclo de recordatorios.
/// </summary>
public sealed class Factura
{
    private Factura()
    {
    }

    /// <summary>
    /// Obtiene el identificador único e inmutable usado para localizar la factura.
    /// </summary>
    public string Id { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene el consecutivo visible para el socio y los operadores.
    /// </summary>
    public string NumeroFactura { get; private set; } = string.Empty;

    /// <summary>
    /// Obtiene el socio responsable de la obligación de pago.
    /// </summary>
    public Socio Socio { get; private set; } = null!;

    /// <summary>
    /// Obtiene el importe pendiente asociado con la factura.
    /// </summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Obtiene la fecha límite que determina la oportunidad del aviso.
    /// </summary>
    public DateTime FechaVencimiento { get; private set; }

    /// <summary>
    /// Obtiene la etapa actual del ciclo de recordatorios.
    /// </summary>
    public EstadoRecordatorio Estado { get; private set; } = EstadoRecordatorio.PrimerRecordatorio;

    /// <summary>
    /// Obtiene el instante UTC del último correo enviado, si existe.
    /// </summary>
    public DateTime? UltimoCorreoEnviado { get; private set; }

    /// <summary>
    /// Crea una factura que cumple las invariantes necesarias para ser almacenada y procesada.
    /// </summary>
    /// <param name="id">Identificador único de la factura.</param>
    /// <param name="numeroFactura">Número comercial visible para el usuario.</param>
    /// <param name="socio">Titular válido al que pertenece la factura.</param>
    /// <param name="valor">Importe positivo que debe pagar el socio.</param>
    /// <param name="fechaVencimiento">Fecha límite establecida para el pago.</param>
    /// <param name="estadoInicial">Etapa inicial del ciclo de recordatorios.</param>
    /// <returns>Una factura consistente y lista para persistirse.</returns>
    public static Factura Crear(
        string id,
        string numeroFactura,
        Socio socio,
        decimal valor,
        DateTime fechaVencimiento,
        EstadoRecordatorio estadoInicial = EstadoRecordatorio.PrimerRecordatorio)
    {
        var factura = new Factura
        {
            Id = ValidarTextoObligatorio(id, nameof(id)),
            Socio = socio ?? throw new ArgumentNullException(nameof(socio))
        };

        factura.ActualizarDatosComerciales(numeroFactura, valor, fechaVencimiento);
        factura.CambiarEstadoAdministrativamente(estadoInicial);
        return factura;
    }

    /// <summary>
    /// Actualiza la información comercial conservando la identidad y la auditoría de recordatorios.
    /// </summary>
    /// <param name="numeroFactura">Número comercial visible para el usuario.</param>
    /// <param name="valor">Importe positivo que debe pagar el socio.</param>
    /// <param name="fechaVencimiento">Fecha límite establecida para el pago.</param>
    public void ActualizarDatosComerciales(string numeroFactura, decimal valor, DateTime fechaVencimiento)
    {
        if (valor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "El valor de la factura debe ser mayor que cero.");
        }

        if (fechaVencimiento == default)
        {
            throw new ArgumentException("La fecha de vencimiento es obligatoria.", nameof(fechaVencimiento));
        }

        NumeroFactura = ValidarTextoObligatorio(numeroFactura, nameof(numeroFactura));
        Valor = valor;
        FechaVencimiento = fechaVencimiento;
    }

    /// <summary>
    /// Permite que un caso de uso administrativo seleccione una etapa válida sin exponer el setter.
    /// </summary>
    /// <param name="estado">Etapa válida que debe quedar disponible para el ciclo de recordatorios.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se produce cuando el valor no pertenece al enum admitido.</exception>
    public void CambiarEstadoAdministrativamente(EstadoRecordatorio estado)
    {
        if (!Enum.IsDefined(estado))
        {
            throw new ArgumentOutOfRangeException(nameof(estado));
        }

        Estado = estado;
    }

    /// <summary>
    /// Registra un correo entregado y aplica la única transición válida desde el estado actual.
    /// </summary>
    /// <param name="fechaEnvioUtc">Instante UTC confirmado para conservar una auditoría consistente.</param>
    /// <returns>El nuevo estado alcanzado después del recordatorio.</returns>
    /// <exception cref="ArgumentException">Se produce cuando la fecha no está expresada en UTC.</exception>
    /// <exception cref="InvalidOperationException">Se produce cuando el ciclo ya está desactivado.</exception>
    public EstadoRecordatorio RegistrarRecordatorioEnviado(DateTime fechaEnvioUtc)
    {
        if (fechaEnvioUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "La fecha del envío debe estar expresada en UTC.",
                nameof(fechaEnvioUtc));
        }

        Estado = Estado switch
        {
            EstadoRecordatorio.PrimerRecordatorio => EstadoRecordatorio.SegundoRecordatorio,
            EstadoRecordatorio.SegundoRecordatorio => EstadoRecordatorio.Desactivado,
            _ => throw new InvalidOperationException(
                "La factura está desactivada y no admite más recordatorios.")
        };

        UltimoCorreoEnviado = fechaEnvioUtc;
        return Estado;
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
