using SistemaSuscripciones.Domain.Entities;
using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Tests;

/// <summary>
/// Verifica que la entidad proteja las reglas internas del ciclo de recordatorios.
/// </summary>
public sealed class FacturaTests
{
    /// <summary>
    /// Comprueba que la entidad avance el estado y registre exactamente la fecha confirmada.
    /// </summary>
    [Fact]
    public void RegistrarRecordatorioEnviado_PrimerRecordatorio_ProtegeLaTransicion()
    {
        var factura = CrearFactura();
        var fechaEnvio = new DateTime(2026, 10, 22, 15, 0, 0, DateTimeKind.Utc);

        var nuevoEstado = factura.RegistrarRecordatorioEnviado(fechaEnvio);

        Assert.Equal(EstadoRecordatorio.SegundoRecordatorio, nuevoEstado);
        Assert.Equal(EstadoRecordatorio.SegundoRecordatorio, factura.Estado);
        Assert.Equal(fechaEnvio, factura.UltimoCorreoEnviado);
    }

    /// <summary>
    /// Comprueba que una factura desactivada rechace cambios incluso si un consumidor omite la validación previa.
    /// </summary>
    [Fact]
    public void RegistrarRecordatorioEnviado_Desactivada_RechazaLaOperacion()
    {
        var factura = CrearFactura(EstadoRecordatorio.Desactivado);

        Assert.Throws<InvalidOperationException>(
            () => factura.RegistrarRecordatorioEnviado(DateTime.UtcNow));
        Assert.Equal(EstadoRecordatorio.Desactivado, factura.Estado);
        Assert.Null(factura.UltimoCorreoEnviado);
    }

    /// <summary>
    /// Comprueba que una factura no pueda nacer con un importe que contradiga la obligación de pago.
    /// </summary>
    [Fact]
    public void Crear_ValorNoPositivo_RechazaLaFactura()
    {
        var socio = CrearSocio();

        Assert.Throws<ArgumentOutOfRangeException>(() => Factura.Crear(
            "factura-1",
            "SUS-001",
            socio,
            0m,
            DateTime.UtcNow.AddDays(30)));
    }

    /// <summary>
    /// Comprueba que un correo inválido no pueda incorporarse al modelo de dominio.
    /// </summary>
    [Fact]
    public void CrearSocio_CorreoInvalido_RechazaElSocio()
    {
        Assert.Throws<ArgumentException>(() => Socio.Crear(
            "socio-1",
            "Socio de prueba",
            "correo-invalido",
            "Premium"));
    }

    private static Factura CrearFactura(
        EstadoRecordatorio estado = EstadoRecordatorio.PrimerRecordatorio) =>
        Factura.Crear(
            "factura-1",
            "SUS-001",
            CrearSocio(),
            150_000m,
            new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc),
            estado);

    private static Socio CrearSocio() =>
        Socio.Crear(
            "socio-1",
            "Socio de prueba",
            "socio@example.com",
            "Premium");
}
