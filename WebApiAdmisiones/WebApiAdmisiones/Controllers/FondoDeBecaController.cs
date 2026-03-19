using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security;
using WebApiAdmisiones.Helpers;
using WebApiAdmisiones.Models;

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

        /// <summary>
        /// Sube el archivo asociado a un ingreso mensual de la declaración jurada del usuario autenticado.
        /// </summary>
        /// <param name="request">JSON con el ID del ingreso mensual, nombre del archivo y bytes del adjunto.</param>
        /// <returns>true si el archivo se guardó correctamente.</returns>
        /// <response code="200">Archivo guardado correctamente.</response>
        /// <response code="400">Request inválido o archivo no permitido.</response>
        /// <response code="404">No se encontró el ingreso mensual indicado.</response>
        /// <response code="403">El ingreso mensual no pertenece al usuario autenticado.</response>
        [HttpPost("SubirArchivoIngreso")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 403)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult SubirArchivoIngreso([FromBody] UploadArchivoIngresoRequest request)
        {
            var fileContent = request.ArchivoAdjunto.Archivo ?? Array.Empty<byte>();
            var fileName = request.ArchivoAdjunto.NombreArchivo ?? string.Empty;
            var result = fondoDeBecaServices.SubirArchivoIngreso(
                _currentUser.GetUserId(),
                request.IdIngresoMensualNF,
                fileContent,
                fileName);

            return ValidateResponse(result);
        }

        /// <summary>
        /// Sube el archivo asociado a un egreso mensual de la declaración jurada del usuario autenticado.
        /// </summary>
        /// <param name="request">JSON con el ID del egreso mensual, nombre del archivo y bytes del adjunto.</param>
        /// <returns>true si el archivo se guardó correctamente.</returns>
        /// <response code="200">Archivo guardado correctamente.</response>
        /// <response code="400">Request inválido o archivo no permitido.</response>
        /// <response code="404">No se encontró el egreso mensual indicado.</response>
        /// <response code="403">El egreso mensual no pertenece al usuario autenticado.</response>
        [HttpPost("SubirArchivoEgreso")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 403)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult SubirArchivoEgreso([FromBody] UploadArchivoEgresoRequest request)
        {
            var fileContent = request.ArchivoAdjunto.Archivo ?? Array.Empty<byte>();
            var fileName = request.ArchivoAdjunto.NombreArchivo ?? string.Empty;
            var result = fondoDeBecaServices.SubirArchivoEgreso(
                _currentUser.GetUserId(),
                request.IdEgresoMensualNF,
                fileContent,
                fileName);

            return ValidateResponse(result);
        }

        /// <summary>
        /// Sube el archivo de reválida asociado a una declaración jurada del usuario autenticado.
        /// </summary>
        /// <param name="request">JSON con el ID de la declaración jurada, nombre del archivo y bytes del adjunto.</param>
        /// <returns>true si el archivo se guardó correctamente.</returns>
        /// <response code="200">Archivo guardado correctamente.</response>
        /// <response code="400">Request inválido o archivo no permitido.</response>
        /// <response code="404">No se encontró la declaración jurada indicada.</response>
        /// <response code="403">La declaracion jurada no pertenece al usuario autenticado.</response>
        [HttpPost("SubirArchivoRevalidaDJ")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 403)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult SubirArchivoRevalidaDj([FromBody] UploadArchivoRevalidaDjRequest request)
        {
            var fileContent = request.ArchivoAdjunto.Archivo ?? Array.Empty<byte>();
            var fileName = request.ArchivoAdjunto.NombreArchivo ?? string.Empty;
            var result = fondoDeBecaServices.SubirArchivoRevalidaDJ(
                _currentUser.GetUserId(),
                request.IdDeclaracionJuradaWeb,
                fileContent,
                fileName);

            return ValidateResponse(result);
        }

        #endregion
    }
}