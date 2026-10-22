using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SistemaSuscripciones.Application.Interfaces;
using SistemaSuscripciones.Domain.Entities;
using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Infrastructure.MongoDb;

/// <summary>
/// Implementa la persistencia de facturas mediante una colección tipada de MongoDB.
/// </summary>
public class FacturaMongoRepository : IFacturaRepository
{
    private readonly IMongoCollection<Factura> _facturas;

    /// <summary>
    /// Inicializa el repositorio usando la conexión y los nombres definidos por configuración externa.
    /// </summary>
    /// <param name="options">Opciones necesarias para localizar la base de datos y la colección de facturas.</param>
    public FacturaMongoRepository(IOptions<MongoDbSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var settings = options.Value;
        var cliente = new MongoClient(settings.ConnectionString);
        var baseDeDatos = cliente.GetDatabase(settings.DatabaseName);

        _facturas = baseDeDatos.GetCollection<Factura>(settings.CollectionName);
    }

    /// <summary>
    /// Recupera de forma asíncrona todas las facturas para atender la consulta general de la API.
    /// </summary>
    /// <returns>Una tarea que contiene la secuencia completa de facturas almacenadas.</returns>
    public async Task<IEnumerable<Factura>> ObtenerTodasAsync()
    {
        return await _facturas
            .Find(Builders<Factura>.Filter.Empty)
            .ToListAsync();
    }

    /// <summary>
    /// Consulta y transmite de forma asíncrona solamente las facturas activas para reducir transferencia y uso de memoria.
    /// </summary>
    /// <param name="cancellationToken">Token que permite cancelar la consulta y la lectura del cursor de MongoDB.</param>
    /// <returns>Un flujo asíncrono alimentado por el cursor de MongoDB con las facturas no desactivadas.</returns>
    public async IAsyncEnumerable<Factura> ObtenerPendientesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var filtro = Builders<Factura>.Filter.Ne(
            factura => factura.Estado,
            EstadoRecordatorio.Desactivado);

        using var cursor = await _facturas.FindAsync(
            filtro,
            cancellationToken: cancellationToken);

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var factura in cursor.Current)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return factura;
            }
        }
    }

    /// <summary>
    /// Busca de forma asíncrona una factura por el identificador de dominio que MongoDB conserva como clave.
    /// </summary>
    /// <param name="id">Identificador único de la factura que se desea recuperar.</param>
    /// <returns>Una tarea que contiene la factura encontrada, o <see langword="null"/> si no existe.</returns>
    public async Task<Factura?> ObtenerPorIdAsync(string id)
    {
        var filtro = Builders<Factura>.Filter.Eq(factura => factura.Id, id);
        return await _facturas.Find(filtro).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Inserta de forma asíncrona una factura completa para conservar también su socio embebido.
    /// </summary>
    /// <param name="factura">Nueva factura que debe incorporarse a la colección.</param>
    /// <returns>Una tarea que representa la finalización de la inserción en MongoDB.</returns>
    public async Task CrearAsync(Factura factura)
    {
        await _facturas.InsertOneAsync(factura);
    }

    /// <summary>
    /// Reemplaza de forma asíncrona el documento para conservar el nuevo estado y la auditoría del envío.
    /// </summary>
    /// <param name="factura">Factura actualizada que debe sustituir su versión persistida.</param>
    /// <returns>Una tarea que representa la finalización de la actualización en MongoDB.</returns>
    public async Task ActualizarAsync(Factura factura)
    {
        var filtro = Builders<Factura>.Filter.Eq(documento => documento.Id, factura.Id);
        await _facturas.ReplaceOneAsync(filtro, factura);
    }

    /// <summary>
    /// Elimina de forma asíncrona una factura y permite saber si el identificador estaba almacenado.
    /// </summary>
    /// <param name="id">Identificador único de la factura que debe retirarse.</param>
    /// <returns>Una tarea cuyo resultado es verdadero cuando MongoDB eliminó un documento.</returns>
    public async Task<bool> EliminarAsync(string id)
    {
        var filtro = Builders<Factura>.Filter.Eq(factura => factura.Id, id);
        var resultado = await _facturas.DeleteOneAsync(filtro);

        return resultado.DeletedCount > 0;
    }
}
