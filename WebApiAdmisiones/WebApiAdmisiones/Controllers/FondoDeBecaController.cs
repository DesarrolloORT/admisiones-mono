using AppLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [HttpGet("TiposParentesco")]
        public IActionResult GetTiposParentesco()
        {
            var result = fondoDeBecaServices.ObtenerTiposParentesco();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Devuelve los tipos de egreso activos, ordenados por campo Orden.
        /// </summary>
        [HttpGet("TiposEgreso")]
        public IActionResult GetTiposEgreso()
        {
            var result = fondoDeBecaServices.ObtenerTiposEgreso();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Devuelve todos los tipos de vivienda disponibles.
        /// </summary>
        [HttpGet("TiposDeVivienda")]
        public IActionResult GetTiposVivienda()
        {
            var result = fondoDeBecaServices.ObtenerTiposVivienda();
            return ValidateResponse(result);
        }

        #endregion

        #region UNIVERSIDADES

        /// <summary>
        /// Devuelve las universidades disponibles para el país indicado.
        /// Por defecto devuelve las de Uruguay (codigoPais = 1).
        /// </summary>
        [HttpGet("Universidades")]
        public IActionResult GetUniversidades([FromQuery] long codigoPais = 1)
        {
            var result = fondoDeBecaServices.ObtenerUniversidades(codigoPais);
            return ValidateResponse(result);
        }

        #endregion
    }
}

