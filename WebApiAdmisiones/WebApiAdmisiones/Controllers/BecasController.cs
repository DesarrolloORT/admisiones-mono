using AppLogic.DevartDTOs;
using AppLogic.Becas.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security.Authentication;

namespace WebApiAdmisiones.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class BecasController(
        IBecasService becasService,
        ILogger<BecasController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<BecasController>(logger, currentUser)
    {
        #region BECAS

        /// <summary>
        /// Obtiene las inscripciones confirmadas de la persona autenticada que pueden usarse en el flujo de becas.
        /// </summary>
        /// <returns>Inscripciones confirmadas disponibles para postular a becas.</returns>
        /// <response code="200">Inscripciones obtenidas correctamente.</response>
        /// <response code="400">Solicitud invalida o error funcional al obtener las inscripciones.</response>
        [HttpGet("Inscripciones")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>>), 400)]
        public IActionResult ObtenerMisInscripcionesConfirmadas()
        {
            var result = becasService.ObtenerMisInscripcionesConfirmadas(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        #endregion
    }
}
