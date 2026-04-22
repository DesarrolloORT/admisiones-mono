using System.Threading.Tasks;
using AppLogic.ApiClients;
using AppLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Controllers
{
    /// <summary>
    /// Controller de ejemplo que demuestra el flujo completo:
    /// Controller → Service → InscripcionesyPagosApiClient → API Interna
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize] // Requiere que el usuario esté autenticado
    public class EjemploInscripcionesController : ApiBaseController<EjemploInscripcionesController>
    {
        private readonly IConsultarInscripcionesService _consultarInscripcionesService;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="consultarInscripcionesService">Servicio para consultar inscripciones</param>
        /// <param name="logger">Logger</param>
        /// <param name="currentUser">Servicio para obtener el usuario actual</param>
        public EjemploInscripcionesController(
            IConsultarInscripcionesService consultarInscripcionesService,
            ILogger<EjemploInscripcionesController> logger,
            ICurrentUserService currentUser)
            : base(logger, currentUser)
        {
            _consultarInscripcionesService = consultarInscripcionesService;
        }

        /// <summary>
        /// Obtiene las inscripciones de una persona consultando a la API interna.
        /// 
        /// FLUJO:
        /// 1. Controller recibe el request
        /// 2. Llama al servicio ConsultarInscripcionesService
        /// 3. El servicio llama a InscripcionesyPagosApiClient.ObtenerInscripcionesAsync()
        /// 4. ServiceAuthenticationHandler intercepta el request y agrega:
        ///    - Authorization: Bearer {token del usuario desde cookie}
        ///    - X-Service-Token: {token firmado de api-admisiones}
        /// 5. Se envía el HTTP request a la API interna
        /// 6. La respuesta regresa por la misma cadena
        /// </summary>
        /// <param name="codigoPersona">Código de la persona a consultar</param>
        /// <returns>Lista de inscripciones de la persona</returns>
        /// <response code="200">Inscripciones obtenidas exitosamente</response>
        /// <response code="400">Parámetros inválidos</response>
        /// <response code="401">Usuario no autenticado</response>
        /// <response code="404">Persona no encontrada</response>
        /// <response code="500">Error interno del servidor</response>
        /// <response code="503">API de Inscripciones no disponible</response>
        /// <response code="504">Timeout al comunicarse con API de Inscripciones</response>
        [HttpGet("persona/{codigoPersona}/inscripciones")]
        [ProducesResponseType(typeof(OperationResult<InscripcionesListResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<InscripcionesListResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<InscripcionesListResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<InscripcionesListResponse>), 404)]
        [ProducesResponseType(typeof(OperationResult<InscripcionesListResponse>), 500)]
        [ProducesResponseType(typeof(OperationResult<InscripcionesListResponse>), 503)]
        [ProducesResponseType(typeof(OperationResult<InscripcionesListResponse>), 504)]
        public async Task<IActionResult> ObtenerInscripciones([FromRoute] long codigoPersona)
        {
            // Validación simple
            if (codigoPersona <= 0)
            {
                var errorResult = OperationResult<InscripcionesListResponse>.IsFailed(
                    "EJEMPLO_001",
                    nameof(ObtenerInscripciones),
                    "El código de persona debe ser mayor a cero.",
                    400,
                    default!
                );
                return BadRequest(errorResult);
            }

            _logger.LogInformation(
                "Usuario {Usuario} solicitó inscripciones de persona {CodigoPersona}",
                _currentUser.UserId,
                codigoPersona
            );

            // Llamar al servicio (que a su vez llama a la API interna)
            var resultado = await _consultarInscripcionesService.ObtenerInscripcionesDePersonaAsync(codigoPersona);

            // ValidateResponse maneja los códigos HTTP automáticamente según resultado.HttpCode
            return ValidateResponse(resultado);
        }

        /// <summary>
        /// Obtiene las inscripciones del usuario autenticado actual.
        /// Ejemplo de uso del ICurrentUserService para obtener el código del usuario logueado.
        /// </summary>
        /// <returns>Lista de inscripciones del usuario actual</returns>
        /// <response code="200">Inscripciones obtenidas exitosamente</response>
        /// <response code="401">Usuario no autenticado</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet("mis-inscripciones")]
        [ProducesResponseType(typeof(OperationResult<InscripcionesListResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<InscripcionesListResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<InscripcionesListResponse>), 500)]
        public async Task<IActionResult> ObtenerMisInscripciones()
        {
            // Obtener el código de persona del usuario autenticado actual
            var codigoPersonaActual = _currentUser.UserId;

            if (!codigoPersonaActual.HasValue)
            {
                var errorResult = OperationResult<InscripcionesListResponse>.IsFailed(
                    "EJEMPLO_002",
                    nameof(ObtenerMisInscripciones),
                    "No se pudo identificar al usuario autenticado.",
                    401,
                    default!
                );
                return Unauthorized(errorResult);
            }

            _logger.LogInformation(
                "Usuario {Usuario} solicitó sus propias inscripciones",
                codigoPersonaActual.Value
            );

            // Llamar al servicio con el código del usuario actual
            var resultado = await _consultarInscripcionesService
                .ObtenerInscripcionesDePersonaAsync(codigoPersonaActual.Value);

            return ValidateResponse(resultado);
        }
    }
}
