using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using AppLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class CatalogosController(
        ICatalogosService catalogosService,
        ILogger<CatalogosController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<CatalogosController>(logger, currentUser)
    {
        #region CATALOGOS

        /// <summary>
        /// Obtiene el país y sus ciudades asociadas.
        /// </summary>
        /// <param name="id">ID del país.</param>
        /// <returns>País y ciudades.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        /// <response code="404">País no encontrado.</response>
        [HttpGet("Pais")]
        [ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 404)]
        public IActionResult ObtenerPais([FromQuery] long id)
        {
            var result = catalogosService.ObtenerPais(id);
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("TiposDocumentos")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 400)]
        public IActionResult ObtenerTipoDocumentos()
        {
            var result = catalogosService.ObtenerTipoDocumentos();
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("Carreras")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoCarreraResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoCarreraResponse>>), 400)]
        public IActionResult ObtenerCarreras()
        {
            var result = catalogosService.ObtenerCarreras();
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("Comienzos")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoComienzoResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoComienzoResponse>>), 400)]
        public IActionResult ObtenerComienzos([FromQuery] long idCarrera)
        {
            var result = catalogosService.ObtenerComienzos(idCarrera);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los motivos de elección disponibles para la encuesta de admisión.
        /// </summary>
        /// <returns>Lista de motivos.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("MotivosEleccion")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>>), 400)]
        public IActionResult ObtenerMotivosEleccion()
        {
            var result = catalogosService.ObtenerMotivosEleccion();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las publicidades de elección disponibles para la encuesta de admisión.
        /// </summary>
        /// <returns>Lista de publicidades.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("PublicidadesEleccion")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>>), 400)]
        public IActionResult ObtenerPublicidadesEleccion()
        {
            var result = catalogosService.ObtenerPublicidadesEleccion();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los bachilleratos para un año de bachiller dado.
        /// </summary>
        /// <param name="idAnioBachillerato">ID del año de bachillerato.</param>
        /// <returns>Lista de bachilleratos.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("Bachilleratos")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTituloDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTituloDevart>>), 400)]
        public IActionResult ObtenerBachilleratos([FromQuery] long idAnioBachillerato)
        {
            var result = catalogosService.ObtenerBachilleratos(idAnioBachillerato);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los datos de un año de bachiller por su ID.
        /// </summary>
        /// <param name="idAnioBachillerato">ID del año de bachillerato.</param>
        /// <returns>Datos del año de bachiller.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        /// <response code="404">Año de bachillerato no encontrado.</response>
        [HttpGet("AnioBachiller")]
        [ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 404)]
        public IActionResult ObtenerAnioBachiller([FromQuery] long idAnioBachillerato)
        {
            var result = catalogosService.ObtenerAnioBachiller(idAnioBachillerato);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las instituciones educativas para un país y estado dados.
        /// </summary>
        /// <param name="codigoPais">Código del país.</param>
        /// <param name="codigoEstado">Código del estado/departamento.</param>
        /// <returns>Lista de instituciones educativas.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("Instituciones")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 400)]
        public IActionResult ObtenerInstituciones([FromQuery] long codigoPais, [FromQuery] long codigoEstado)
        {
            var result = catalogosService.ObtenerInstituciones(codigoPais, codigoEstado);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las universidades disponibles para pregrado.
        /// </summary>
        /// <returns>Lista de universidades.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("Universidades")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 400)]
        public IActionResult ObtenerUniversidades()
        {
            var result = catalogosService.ObtenerUniversidades();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los productos elegibles para beca de la persona autenticada:
        /// combina inscripciones realizadas, pendientes en workflow e intereses activos.
        /// Un registro por producto (el más antiguo por fecha de inscripción).
        /// </summary>
        /// <returns>Lista de productos beca.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("ProductosBeca")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoBeca>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoBeca>>), 400)]
        public IActionResult ObtenerProductosBeca()
        {
            var result = catalogosService.ObtenerProductosBeca(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los fondos de beca disponibles según el nivel de un producto.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <returns>Lista de tipos de descuento (fondos de beca).</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("FondosDeBecaPorProducto")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTipoDescuentoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTipoDescuentoDevart>>), 400)]
        public IActionResult ObtenerFondosDeBecaPorProducto([FromQuery] long idProducto)
        {
            var result = catalogosService.ObtenerFondosDeBecaPorProducto(idProducto);
            return ValidateResponse(result);
        }

        #endregion
    }
}
