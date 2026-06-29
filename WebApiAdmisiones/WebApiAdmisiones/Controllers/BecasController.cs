using AppLogic.DevartDTOs;
using AppLogic.IServices.Becas;
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
