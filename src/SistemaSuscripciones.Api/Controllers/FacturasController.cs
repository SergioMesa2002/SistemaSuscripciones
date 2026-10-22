using Microsoft.AspNetCore.Mvc;
using SistemaSuscripciones.Application.DTOs;
using SistemaSuscripciones.Application.Interfaces;

namespace SistemaSuscripciones.Api.Controllers;

/// <summary>
/// Expone las operaciones HTTP para consultar facturas y gestionar sus recordatorios de pago.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FacturasController : ControllerBase
{
    private readonly IFacturaService _facturaService;
    private readonly IRecordatorioService _recordatorioService;
    private readonly ILogger<FacturasController> _logger;

    /// <summary>
    /// Inicializa el controlador con los casos de uso y consultas requeridos por la API.
    /// </summary>
    /// <param name="facturaService">Caso de uso utilizado para consultar y mantener facturas.</param>
    /// <param name="recordatorioService">Servicio que coordina el envío y las transiciones de estado.</param>
    /// <param name="logger">Registro técnico utilizado sin trasladar detalles sensibles a las respuestas.</param>
    public FacturasController(
        IFacturaService facturaService,
        IRecordatorioService recordatorioService,
        ILogger<FacturasController> logger)
    {
        _facturaService = facturaService;
        _recordatorioService = recordatorioService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el resumen de todas las facturas disponibles.
    /// </summary>
    /// <returns>Una respuesta asíncrona con los datos necesarios para la futura tabla de facturas.</returns>
    /// <response code="200">Devuelve la lista de facturas, que puede estar vacía.</response>
    /// <response code="500">Indica que ocurrió un fallo inesperado al consultar la información.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FacturaResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<FacturaResumenDto>>> ObtenerTodasAsync()
    {
        try
        {
            return Ok(await _facturaService.ObtenerTodasAsync());
        }
        catch (Exception exception)
        {
            RegistrarFalloInesperado(exception, "consultar las facturas");
            return CrearProblemaInterno();
        }
    }

    /// <summary>
    /// Crea una factura con identificadores nuevos para dejarla disponible para futuros recordatorios.
    /// </summary>
    /// <param name="solicitud">Datos comerciales y estado inicial de la nueva factura.</param>
    /// <returns>Una respuesta asíncrona con el resumen de la factura creada.</returns>
    /// <response code="201">Indica que la factura fue almacenada correctamente.</response>
    /// <response code="400">Indica que uno o más datos de entrada no son válidos.</response>
    /// <response code="500">Indica que ocurrió un fallo inesperado durante la creación.</response>
    [HttpPost]
    [ProducesResponseType(typeof(FacturaResumenDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FacturaResumenDto>> CrearAsync(GuardarFacturaDto solicitud)
    {
        try
        {
            var factura = await _facturaService.CrearAsync(solicitud);
            return StatusCode(StatusCodes.Status201Created, factura);
        }
        catch (Exception exception)
        {
            RegistrarFalloInesperado(exception, "crear una factura");
            return CrearProblemaInterno();
        }
    }

    /// <summary>
    /// Actualiza los datos editables de una factura sin perder la auditoría de su último correo.
    /// </summary>
    /// <param name="id">Identificador de la factura que se desea modificar.</param>
    /// <param name="solicitud">Nuevos datos comerciales y estado de la factura.</param>
    /// <returns>Una respuesta asíncrona con el resumen actualizado.</returns>
    /// <response code="200">Devuelve la factura después de aplicar la actualización.</response>
    /// <response code="400">Indica que uno o más datos de entrada no son válidos.</response>
    /// <response code="404">Indica que no existe una factura con el identificador solicitado.</response>
    /// <response code="500">Indica que ocurrió un fallo inesperado durante la actualización.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(FacturaResumenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FacturaResumenDto>> ActualizarAsync(
        string id,
        GuardarFacturaDto solicitud)
    {
        try
        {
            var factura = await _facturaService.ActualizarAsync(id, solicitud);

            if (factura is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Factura no encontrada.",
                    Detail = $"No existe una factura con el identificador '{id}'.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(factura);
        }
        catch (Exception exception)
        {
            RegistrarFalloInesperado(exception, "actualizar una factura");
            return CrearProblemaInterno();
        }
    }

    /// <summary>
    /// Elimina definitivamente una factura que ya no debe permanecer en el sistema.
    /// </summary>
    /// <param name="id">Identificador de la factura que se desea eliminar.</param>
    /// <returns>Una respuesta asíncrona sin contenido cuando la eliminación se completa.</returns>
    /// <response code="204">Indica que la factura fue eliminada correctamente.</response>
    /// <response code="404">Indica que no existe una factura con el identificador solicitado.</response>
    /// <response code="500">Indica que ocurrió un fallo inesperado durante la eliminación.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EliminarAsync(string id)
    {
        try
        {
            var eliminada = await _facturaService.EliminarAsync(id);

            if (!eliminada)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Factura no encontrada.",
                    Detail = $"No existe una factura con el identificador '{id}'.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return NoContent();
        }
        catch (Exception exception)
        {
            RegistrarFalloInesperado(exception, "eliminar una factura");
            return CrearProblemaInterno();
        }
    }

    /// <summary>
    /// Envía el siguiente recordatorio permitido para una factura específica.
    /// </summary>
    /// <param name="id">Identificador de la factura que debe procesarse.</param>
    /// <returns>Una respuesta asíncrona con el resultado y el nuevo estado de la factura.</returns>
    /// <response code="200">Indica que el correo se envió y la factura se actualizó.</response>
    /// <response code="400">Indica que la factura no existe o ya está desactivada.</response>
    /// <response code="500">Indica que ocurrió un fallo inesperado durante el procesamiento.</response>
    [HttpPost("{id}/enviar-recordatorio")]
    [ProducesResponseType(typeof(ResultadoRecordatorioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultadoRecordatorioDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultadoRecordatorioDto>> EnviarRecordatorioAsync(string id)
    {
        try
        {
            var resultado = await _recordatorioService.EnviarAsync(id);
            return resultado.Exito ? Ok(resultado) : BadRequest(resultado);
        }
        catch (Exception exception)
        {
            RegistrarFalloInesperado(exception, "enviar un recordatorio");
            return CrearProblemaInterno();
        }
    }

    /// <summary>
    /// Procesa todas las facturas que todavía tienen recordatorios activos.
    /// </summary>
    /// <returns>Una respuesta asíncrona con el resultado obtenido para cada factura procesada.</returns>
    /// <response code="200">Devuelve los resultados del procesamiento, aunque no existan facturas pendientes.</response>
    /// <response code="500">Indica que ocurrió un fallo inesperado durante el procesamiento masivo.</response>
    [HttpPost("procesar-pendientes")]
    [ProducesResponseType(typeof(IEnumerable<ResultadoRecordatorioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ResultadoRecordatorioDto>>> ProcesarPendientesAsync()
    {
        try
        {
            var resultados = await _recordatorioService.ProcesarPendientesAsync();
            return Ok(resultados);
        }
        catch (Exception exception)
        {
            RegistrarFalloInesperado(exception, "procesar las facturas pendientes");
            return CrearProblemaInterno();
        }
    }

    private ObjectResult CrearProblemaInterno()
    {
        return Problem(
            title: "No fue posible completar la operación.",
            detail: "Ocurrió un error inesperado. Intenta nuevamente más tarde.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    private void RegistrarFalloInesperado(Exception exception, string operacion)
    {
        _logger.LogError(
            "Fallo inesperado al {Operacion}. Tipo de error: {TipoError}.",
            operacion,
            exception.GetType().Name);
    }
}
