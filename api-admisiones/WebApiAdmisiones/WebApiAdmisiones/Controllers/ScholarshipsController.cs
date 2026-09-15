using AppLogic.Scholarships.Dtos;
using AppLogic.Scholarships.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security.Authentication;

namespace WebApiAdmisiones.Controllers
{
    [Authorize]
    [ApiController]
    [Route("scholarships")]
    public class ScholarshipsController(
        IScholarshipService becasService,
        ILogger<ScholarshipsController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<ScholarshipsController>(logger, currentUser)
    {
        #region BECAS

        /// <summary>
        /// Obtiene las inscripciones confirmadas de la persona autenticada que pueden usarse en el flujo de becas.
        /// </summary>
        /// <returns>Inscripciones confirmadas disponibles para postular a becas.</returns>
        /// <response code="200">Inscripciones obtenidas correctamente.</response>
        /// <response code="400">Solicitud invalida o error funcional al obtener las inscripciones.</response>
        [HttpGet("enrollments")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<ConfirmedEnrollmentResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<ConfirmedEnrollmentResponse>>), 400)]
        public IActionResult GetMyConfirmedEnrollments()
        {
            var result = becasService.GetMyConfirmedEnrollments(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        #endregion
    }
}
