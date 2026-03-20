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
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Pais")]
        [ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 400)]
        public IActionResult ObtenerPais([FromQuery] long id)
        {
            var result = catalogosService.ObtenerPais(id);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene la lista de países.
        /// </summary>
        /// <returns>Lista de países.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Paises")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 400)]
        public IActionResult ObtenerPaises()
        {
            var result = catalogosService.ObtenerPaises();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene el listado de tipos de documento disponibles.
        /// </summary>
        /// <returns>OperationResult con colección de DtoAcaTipoDocumentoDevart.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("TipoDocumentos")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 400)]
        public IActionResult ObtenerTipoDocumentos()
        {
            var result = catalogosService.ObtenerTipoDocumentos();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los motivos de elección disponibles para la encuesta de admisión.
        /// </summary>
        /// <returns>Lista de motivos.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("MotivosEleccion")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>>), 204)]
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
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("PublicidadesEleccion")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>>), 204)]
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
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Bachilleratos")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTituloDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTituloDevart>>), 204)]
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
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("AnioBachiller")]
        [ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 204)]
        [ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 400)]
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
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Instituciones")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 204)]
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
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Universidades")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 204)]
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
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("ProductosBeca")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoBeca>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoBeca>>), 400)]
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
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("FondosDeBecaPorProducto")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTipoDescuentoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTipoDescuentoDevart>>), 204)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTipoDescuentoDevart>>), 400)]
        public IActionResult ObtenerFondosDeBecaPorProducto([FromQuery] long idProducto)
        {
            var result = catalogosService.ObtenerFondosDeBecaPorProducto(idProducto);
            return ValidateResponse(result);
        }

        #endregion
    }
}
