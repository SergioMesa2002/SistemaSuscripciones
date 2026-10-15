using System.Reflection;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using SistemaSuscripciones.Application.Interfaces;
using SistemaSuscripciones.Application.Services;
using SistemaSuscripciones.Infrastructure;

var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? Environments.Production;

if (string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase))
{
    var envFilePath = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "src", "SistemaSuscripciones.Api", ".env"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"))
    }.Distinct().FirstOrDefault(File.Exists);

    if (envFilePath is not null)
    {
        Env.NoClobber().Load(envFilePath);
    }
}

var builder = WebApplication.CreateBuilder(args);

// Se agregan al final para que el entorno, incluido .env en desarrollo, prevalezca sobre appsettings.json.
builder.Configuration.AddEnvironmentVariables();

const string AngularCorsPolicy = "AngularDevelopment";

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IRecordatorioService, RecordatorioService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        await Results.Problem(
            title: "No fue posible completar la operación.",
            detail: "Ocurrió un error inesperado. Revisa la configuración e intenta nuevamente.",
            statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(context);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(AngularCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
