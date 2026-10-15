using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaSuscripciones.Application.Interfaces;
using SistemaSuscripciones.Infrastructure.Email;
using SistemaSuscripciones.Infrastructure.MongoDb;

namespace SistemaSuscripciones.Infrastructure;

/// <summary>
/// Centraliza el registro de adaptadores y opciones para mantener la composición fuera de la lógica de negocio.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra la configuración y las implementaciones de MongoDB y SMTP en el contenedor de forma reutilizable.
    /// </summary>
    /// <param name="services">Colección donde se incorporarán las dependencias de infraestructura.</param>
    /// <param name="configuration">Origen externo desde el que se enlazarán las opciones de conexión.</param>
    /// <returns>La misma colección para permitir encadenar otros registros durante el arranque.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection(MongoDbSettings.Seccion));
        services.Configure<SmtpSettings>(
            configuration.GetSection(SmtpSettings.Seccion));

        services.AddSingleton<IFacturaRepository, FacturaMongoRepository>();
        services.AddTransient<IEmailService, SmtpEmailService>();

        return services;
    }
}
