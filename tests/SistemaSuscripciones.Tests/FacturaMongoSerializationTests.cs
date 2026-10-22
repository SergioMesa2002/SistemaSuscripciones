using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using SistemaSuscripciones.Domain.Entities;
using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Tests;

/// <summary>
/// Verifica la compatibilidad del encapsulamiento de dominio con la serialización de MongoDB.
/// </summary>
public sealed class FacturaMongoSerializationTests
{
    /// <summary>
    /// Comprueba que MongoDB pueda reconstruir el estado y la auditoría aunque sus setters no sean públicos.
    /// </summary>
    [Fact]
    public void SerializarYDeserializar_PropiedadesProtegidas_ConservaLosValores()
    {
        var fechaEnvio = new DateTime(2026, 10, 22, 16, 0, 0, DateTimeKind.Utc);
        var socio = Socio.Crear(
            "socio-300",
            "Socio MongoDB",
            "mongo@example.com",
            "Premium");
        var factura = Factura.Crear(
            "factura-serializada",
            "SUS-300",
            socio,
            300_000m,
            fechaEnvio.AddDays(30));
        factura.RegistrarRecordatorioEnviado(fechaEnvio);

        var bson = factura.ToBson();
        var restaurada = BsonSerializer.Deserialize<Factura>(bson);

        Assert.Equal(EstadoRecordatorio.SegundoRecordatorio, restaurada.Estado);
        Assert.Equal(fechaEnvio, restaurada.UltimoCorreoEnviado);
        Assert.Equal(factura.Id, restaurada.Id);
        Assert.Equal(factura.NumeroFactura, restaurada.NumeroFactura);
        Assert.Equal(factura.Valor, restaurada.Valor);
        Assert.Equal(factura.Socio.Correo, restaurada.Socio.Correo);
    }
}
