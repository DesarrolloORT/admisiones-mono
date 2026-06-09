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

        /// <summary>
        /// Obtiene la aceptación del reglamento estudiantil de la persona autenticada.
        /// </summary>
        /// <returns>Datos de aceptación del reglamento.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="404">No se encontró aceptación del reglamento para la persona.</response>
        /// <response code="400">Solicitud inválida.</response>
        //[HttpGet("AceptacionReglamentoEstudiantil")]
        //[ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 200)]
        //[ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 400)]
        //[ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 404)]
        //public IActionResult ObtenerAceptacionReglamentoEstudiantil()
        //{
        //    var result = becasService.ObtenerAceptacionReglamentoEstudiantil(_currentUser.GetUserId());
        //    return ValidateResponse(result);
        //}

        /// <summary>
        /// Registra la aceptación del reglamento estudiantil para la persona autenticada.
        /// Toma el producto y el comienzo desde la encuesta inicial de admisión vigente.
        /// </summary>
        /// <returns>Datos de la aceptación registrada.</returns>
        /// <response code="200">Aceptación registrada correctamente.</response>
        /// <response code="400">La encuesta no contiene producto o comienzo válidos.</response>
        /// <response code="404">No se encontró la persona o la encuesta inicial de admisión.</response>
        /// <response code="409">Ya existe una aceptación registrada para la persona, producto y comienzo.</response>
        //[HttpPost("AceptacionReglamentoEstudiantil")]
        //[ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 200)]
        //[ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 400)]
        //[ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 404)]
        //[ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 409)]
        //public IActionResult RegistrarAceptacionReglamentoEstudiantil()
        //{
        //    var result = becasService.RegistrarAceptacionReglamentoEstudiantil(_currentUser.GetUserId());
        //    return ValidateResponse(result);
        //}

        ///// <summary>
        ///// Obtiene los fondos de beca vigentes para el alumno autenticado,
        ///// dado un producto y proceso.
        ///// </summary>
        ///// <param name="idProducto">ID del producto.</param>
        ///// <param name="idProceso">ID del proceso.</param>
        ///// <returns>Lista de pruebas de beca vigentes.</returns>
        ///// <response code="200">Datos obtenidos correctamente.</response>
        ///// <response code="400">Producto inválido.</response>
        //[HttpGet("FondosDeBecaVigentes")]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPruebaDevart>>), 200)]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPruebaDevart>>), 400)]
        //public IActionResult ObtenerFondosDeBecaVigentes([FromQuery] long idProducto, [FromQuery] long idProceso)
        //{
        //    var result = becasService.ObtenerFondosDeBecaVigentes(idProducto, idProceso, _currentUser.GetUserId());
        //    return ValidateResponse(result);
        //}

        #endregion
    }
}
