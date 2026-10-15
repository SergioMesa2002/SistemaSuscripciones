using SistemaSuscripciones.Application.DTOs;
using SistemaSuscripciones.Application.Interfaces;
using SistemaSuscripciones.Domain.Entities;
using SistemaSuscripciones.Domain.Enums;

namespace SistemaSuscripciones.Application.Services;

/// <summary>
/// Coordina el envío de correos y la actualización segura del ciclo de recordatorios.
/// </summary>
public class RecordatorioService : IRecordatorioService
{
    private readonly IFacturaRepository _facturaRepository;
    private readonly IEmailService _emailService;

    /// <summary>
    /// Inicializa el servicio con abstracciones que permiten ejecutar la lógica sin depender de infraestructura concreta.
    /// </summary>
    /// <param name="facturaRepository">Repositorio utilizado para consultar y conservar facturas.</param>
    /// <param name="emailService">Servicio utilizado para entregar los avisos de pago.</param>
    public RecordatorioService(
        IFacturaRepository facturaRepository,
        IEmailService emailService)
    {
        _facturaRepository = facturaRepository ?? throw new ArgumentNullException(nameof(facturaRepository));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    /// <summary>
    /// Busca y procesa de forma asíncrona el siguiente recordatorio de una factura.
    /// </summary>
    /// <param name="id">Identificador de la factura que se desea procesar.</param>
    /// <returns>Una tarea que contiene el resultado del envío y el estado alcanzado.</returns>
    public async Task<ResultadoRecordatorioDto> EnviarAsync(string id)
    {
        var factura = await _facturaRepository.ObtenerPorIdAsync(id);

        if (factura is null)
        {
            return new ResultadoRecordatorioDto
            {
                Exito = false,
                Mensaje = $"No se encontró una factura con el identificador '{id}'.",
                FacturaId = id
            };
        }

        return await ProcesarFacturaAsync(factura);
    }

    /// <summary>
    /// Consulta y procesa de forma asíncrona únicamente las facturas cuyo ciclo de avisos permanece activo.
    /// </summary>
    /// <returns>Una tarea que contiene un resultado por cada factura activa procesada.</returns>
    public async Task<IEnumerable<ResultadoRecordatorioDto>> ProcesarPendientesAsync()
    {
        var facturas = await _facturaRepository.ObtenerTodasAsync();
        var resultados = new List<ResultadoRecordatorioDto>();

        foreach (var factura in facturas.Where(factura => factura.Estado != EstadoRecordatorio.Desactivado))
        {
            resultados.Add(await ProcesarFacturaAsync(factura));
        }

        return resultados;
    }

    private async Task<ResultadoRecordatorioDto> ProcesarFacturaAsync(Factura factura)
    {
        if (factura.Estado == EstadoRecordatorio.Desactivado)
        {
            return new ResultadoRecordatorioDto
            {
                Exito = false,
                Mensaje = "La factura está desactivada y no admite más recordatorios.",
                FacturaId = factura.Id,
                NuevoEstado = factura.Estado
            };
        }

        await _emailService.EnviarRecordatorioAsync(factura);

        // El estado avanza solo después del envío para conservarlo intacto si el proveedor de correo falla.
        factura.Estado = factura.Estado switch
        {
            EstadoRecordatorio.PrimerRecordatorio => EstadoRecordatorio.SegundoRecordatorio,
            EstadoRecordatorio.SegundoRecordatorio => EstadoRecordatorio.Desactivado,
            _ => throw new InvalidOperationException("El estado de la factura no permite enviar recordatorios.")
        };

        factura.UltimoCorreoEnviado = DateTime.UtcNow;
        await _facturaRepository.ActualizarAsync(factura);

        return new ResultadoRecordatorioDto
        {
            Exito = true,
            Mensaje = "El recordatorio se envió y la factura se actualizó correctamente.",
            FacturaId = factura.Id,
            NuevoEstado = factura.Estado
        };
    }
}
