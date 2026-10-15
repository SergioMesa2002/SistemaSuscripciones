namespace SistemaSuscripciones.Infrastructure.MongoDb;

/// <summary>
/// Reúne los valores necesarios para conectar la aplicación con el almacenamiento de facturas en MongoDB.
/// </summary>
public class MongoDbSettings
{
    /// <summary>
    /// Identifica la sección de configuración desde la que se enlazan las opciones de MongoDB.
    /// </summary>
    public const string Seccion = "MongoDb";

    /// <summary>
    /// Obtiene o establece la cadena de conexión proporcionada por el entorno de ejecución.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el nombre de la base de datos que contiene la información del sistema.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la colección donde se conservan las facturas con sus socios embebidos.
    /// </summary>
    public string CollectionName { get; set; } = "facturas";
}
