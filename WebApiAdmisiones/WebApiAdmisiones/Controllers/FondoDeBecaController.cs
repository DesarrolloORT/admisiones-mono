using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class FondoDeBecaController(
        IFondoDeBecaServices fondoDeBecaServices,
        ILogger<FondoDeBecaController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<FondoDeBecaController>(logger, currentUser)
    {
        #region TIPOS DECLARACIÓN JURADA

        /// <summary>
        /// Devuelve todos los tipos de parentesco disponibles.
        /// </summary>
        /// <returns>Colección de tipos de parentesco.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("TiposParentesco")]
        public IActionResult GetTiposParentesco()
        {
            var result = fondoDeBecaServices.ObtenerTiposParentesco();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Devuelve los tipos de egreso activos, ordenados por campo Orden.
        /// </summary>
        /// <returns>Colección de tipos de egreso activos.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("TiposEgreso")]
        public IActionResult GetTiposEgreso()
        {
            var result = fondoDeBecaServices.ObtenerTiposEgreso();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Devuelve todos los tipos de vivienda disponibles.
        /// </summary>
        /// <returns>Colección de tipos de vivienda.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("TiposDeVivienda")]
        public IActionResult GetTiposVivienda()
        {
            var result = fondoDeBecaServices.ObtenerTiposVivienda();
            return ValidateResponse(result);
        }

        #endregion

        #region UNIVERSIDADES

        /// <summary>
        /// Devuelve las universidades disponibles para el paí­s indicado.
        /// </summary>
        /// <param name="codigoPais">Código del paí­s. Por defecto devuelve Uruguay (1).</param>
        /// <returns>Colección de universidades para el país indicado.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Universidades")]
        public IActionResult GetUniversidades([FromQuery] long codigoPais = 1)
        {
            var result = fondoDeBecaServices.ObtenerUniversidades(codigoPais);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Devuelve el resumen de formularios de declaración jurada web vigentes del usuario autenticado.
        /// </summary>
        /// <returns>Colección de formularios vigentes resumidos.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("FormularioDeclaracionJuradaWeb")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTODeclaracionJuradaAdmisiones>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTODeclaracionJuradaAdmisiones>>), 404)]
        public IActionResult GetFormulariosDeclaracionJuradaWeb()
        {
            var result = fondoDeBecaServices.ObtenerFormulariosDeclaracionJuradaWeb(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Devuelve el detalle de un formulario de declaración jurada web del usuario autenticado.
        /// </summary>
        /// <param name="idInscriptoPrueba">ID de la inscripción a prueba asociada al formulario.</param>
        /// <returns>Detalle del formulario solicitado.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("FormularioDeclaracionJuradaWebDetalle")]
        [ProducesResponseType(typeof(OperationResult<DtoDeclaracionJuradaWebDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoDeclaracionJuradaWebDevart>), 404)]
        public IActionResult GetFormularioDeclaracionJuradaWebDetalle([FromQuery] long idInscriptoPrueba)
        {
            var result = fondoDeBecaServices.ObtenerFormularioDeclaracionJuradaWebDetalle(_currentUser.GetUserId(), idInscriptoPrueba);
            return ValidateResponse(result);
        }

        #endregion
    }
}