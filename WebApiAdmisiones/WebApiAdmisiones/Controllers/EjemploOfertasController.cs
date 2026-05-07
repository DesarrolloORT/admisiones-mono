using AppLogic.ApiClients;
using AppLogic.DevartDTOs;
using AppLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Utilities;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class EjemploOfertasController : ApiBaseController<EjemploOfertasController>
    {
        private readonly IOfertasInscripcionService _ofertasInscripcionService;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="ofertasInscripcionService">Servicio de ofertas de inscripción</param>
        /// <param name="logger">Logger para trazabilidad</param>
        /// <param name="currentUser">Servicio para obtener el usuario actual desde el JWT</param>
        public EjemploOfertasController(
            IOfertasInscripcionService ofertasInscripcionService,
            ILogger<EjemploOfertasController> logger,
            ICurrentUserService currentUser)
            : base(logger, currentUser)
        {
            _ofertasInscripcionService = ofertasInscripcionService;
        }

        [HttpGet("Ofertas")]
        [ProducesResponseType(typeof(OperationResult<OfertasInscripcionResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<OfertasInscripcionResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<OfertasInscripcionResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<OfertasInscripcionResponse>), 422)]
        public async Task<IActionResult> ObtenerMisOfertas(
            [FromQuery] long idProducto,
            [FromQuery] long idComienzo,
            [FromQuery] long idTurno)
        {
            // Obtener el código de persona del usuario autenticado actual
            var codigoPersonaActual = _currentUser.UserId;

            if (!codigoPersonaActual.HasValue)
            {
                var errorResult = OperationResult<OfertasInscripcionResponse>.IsFailed(
                    "EJEMPLO_OFERTAS_03",
                    nameof(ObtenerMisOfertas),
                    "No se pudo identificar al usuario autenticado.",
                    401,
                    default!
                );
                return Unauthorized(errorResult);
            }

            _logger.LogInformation(
                "Usuario {UsuarioId} solicitó sus propias ofertas - " +
                "Producto: {IdProducto}, Comienzo: {IdComienzo}, Turno: {IdTurno}",
                codigoPersonaActual.Value,
                idProducto,
                idComienzo,
                idTurno
            );

            // Reutilizar la misma lógica usando el código del usuario actual
            var resultado = await _ofertasInscripcionService.ObtenerOfertasParaPersonaAsync(
                codigoPersonaActual.Value,
                idProducto,
                idComienzo,
                idTurno
            );

            return ValidateResponse(resultado);
        }

        
    }
}
