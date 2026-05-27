using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using AppLogic.ApiClients;
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
        //[HttpGet("Pais")]
        //[ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 200)]
        //[ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 400)]
        //[ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 404)]
        //public IActionResult ObtenerPais([FromQuery] long id)
        //{
        //    var result = catalogosService.ObtenerPais(id);
        //    return ValidateResponse(result);
        //}

        /// <summary>
        /// Lista paises, estados y ciudades para formularios de admision.
        /// </summary>
        /// <remarks>
        /// Endpoint publico para poblar combos de ubicacion. Devuelve una respuesta liviana con codigos y nombres, manteniendo Uruguay primero, luego paises por nombre, y estados/ciudades ordenados alfabeticamente.
        /// </remarks>
        /// <returns>Paises con sus estados y ciudades disponibles.</returns>
        /// <response code="200">Catalogo obtenido correctamente.</response>
        /// <response code="400">Solicitud invalida.</response>
        [AllowAnonymous]
        [HttpGet("PaisesEstadosCiudades")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>), 400)]
        public IActionResult ObtenerPaisesEstadosCiudades()
        {
            var result = catalogosService.ObtenerPaisesEstadosCiudades();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Lista los tipos de documento aceptados por admisiones.
        /// </summary>
        /// <remarks>
        /// Endpoint publico para poblar el combo de tipo de documento en registro, login o recuperacion de acceso.
        /// </remarks>
        /// <returns>Tipos de documento disponibles.</returns>
        /// <response code="200">Catalogo obtenido correctamente.</response>
        /// <response code="400">Solicitud invalida.</response>
        //[AllowAnonymous]
        //[HttpGet("TiposDocumentos")]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 200)]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 400)]
        //public IActionResult ObtenerTipoDocumentos()
        //{
        //    var result = catalogosService.ObtenerTipoDocumentos();
        //    return ValidateResponse(result);
        //}

        /// <summary>
        /// Lista las carreras vigentes para el registro de admision.
        /// </summary>
        /// <remarks>
        /// Endpoint publico para poblar la seleccion inicial de carrera/producto. Devuelve identificadores y nombres necesarios para que el front luego consulte comienzos.
        /// </remarks>
        /// <returns>Carreras vigentes disponibles para admision.</returns>
        /// <response code="200">Catalogo obtenido correctamente.</response>
        /// <response code="400">Solicitud invalida.</response>
        [AllowAnonymous]
        [HttpGet("Carreras")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoCarreraResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoCarreraResponse>>), 400)]
        public IActionResult ObtenerCarreras()
        {
            var result = catalogosService.ObtenerCarreras();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Lista los comienzos habilitados para una carrera.
        /// </summary>
        /// <remarks>
        /// Endpoint publico para poblar el combo de comienzo/proceso luego de seleccionar una carrera. El parametro idCarrera debe ser el IdProducto recibido desde el endpoint de carreras.
        /// </remarks>
        /// <param name="idCarrera">Identificador de la carrera/producto seleccionado.</param>
        /// <returns>Comienzos habilitados para la carrera indicada.</returns>
        /// <response code="200">Catalogo obtenido correctamente.</response>
        /// <response code="400">Carrera invalida o solicitud invalida.</response>
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
        /// Lista las ofertas disponibles para una carrera y proceso.
        /// </summary>
        /// <remarks>
        /// Endpoint para obtener las ofertas disponibles en Inscripciones y Pagos.
        /// </remarks>
        /// <param name="idCarrera">Identificador de la carrera/producto seleccionado.</param>
        /// <param name="idProceso">Identificador del proceso/comienzo seleccionado.</param>
        /// <returns>Ofertas disponibles para la combinacion indicada.</returns>
        /// <response code="200">Catalogo obtenido correctamente.</response>
        /// <response code="400">Carrera, proceso o solicitud invalida.</response>
        [HttpGet("Turnos")]
        [ProducesResponseType(typeof(OperationResult<List<OfertaInscripcionDto>>), 200)]
        [ProducesResponseType(typeof(OperationResult<List<OfertaInscripcionDto>>), 400)]
        public async Task<IActionResult> ObtenerTurnos([FromQuery] long idCarrera, [FromQuery] long idProceso)
        {
            var result = await catalogosService.ObtenerTurnos(idCarrera, idProceso);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los motivos de elección disponibles para la encuesta de admisión.
        /// </summary>
        /// <returns>Lista de motivos.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        //[HttpGet("MotivosEleccion")]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>>), 200)]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoMotivoOpcionesAdmisionDevart>>), 400)]
        //public IActionResult ObtenerMotivosEleccion()
        //{
        //    var result = catalogosService.ObtenerMotivosEleccion();
        //    return ValidateResponse(result);
        //}

        /// <summary>
        /// Obtiene las publicidades de elección disponibles para la encuesta de admisión.
        /// </summary>
        /// <returns>Lista de publicidades.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        //[HttpGet("PublicidadesEleccion")]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>>), 200)]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPublicidadOpcionesAdmisionDevart>>), 400)]
        //public IActionResult ObtenerPublicidadesEleccion()
        //{
        //    var result = catalogosService.ObtenerPublicidadesEleccion();
        //    return ValidateResponse(result);
        //}

        /// <summary>
        /// Obtiene los bachilleratos para un año de bachiller dado.
        /// </summary>
        /// <param name="idAnioBachillerato">ID del año de bachillerato.</param>
        /// <returns>Lista de bachilleratos.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        //[HttpGet("Bachilleratos")]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTituloDevart>>), 200)]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTituloDevart>>), 400)]
        //public IActionResult ObtenerBachilleratos([FromQuery] long idAnioBachillerato)
        //{
        //    var result = catalogosService.ObtenerBachilleratos(idAnioBachillerato);
        //    return ValidateResponse(result);
        //}

        /// <summary>
        /// Obtiene los datos de un año de bachiller por su ID.
        /// </summary>
        /// <param name="idAnioBachillerato">ID del año de bachillerato.</param>
        /// <returns>Datos del año de bachiller.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        /// <response code="404">Año de bachillerato no encontrado.</response>
        //[HttpGet("AnioBachiller")]
        //[ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 200)]
        //[ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 400)]
        //[ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 404)]
        //public IActionResult ObtenerAnioBachiller([FromQuery] long idAnioBachillerato)
        //{
        //    var result = catalogosService.ObtenerAnioBachiller(idAnioBachillerato);
        //    return ValidateResponse(result);
        //}

        /// <summary>
        /// Obtiene las instituciones educativas para un país y estado dados.
        /// </summary>
        /// <param name="codigoPais">Código del país.</param>
        /// <param name="codigoEstado">Código del estado/departamento.</param>
        /// <returns>Lista de instituciones educativas.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        //[HttpGet("Instituciones")]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 200)]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 400)]
        //public IActionResult ObtenerInstituciones([FromQuery] long codigoPais, [FromQuery] long codigoEstado)
        //{
        //    var result = catalogosService.ObtenerInstituciones(codigoPais, codigoEstado);
        //    return ValidateResponse(result);
        //}

        /// <summary>
        /// Obtiene las universidades disponibles para pregrado.
        /// </summary>
        /// <returns>Lista de universidades.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        //[HttpGet("Universidades")]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 200)]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoEmpresaDevart>>), 400)]
        //public IActionResult ObtenerUniversidades()
        //{
        //    var result = catalogosService.ObtenerUniversidades();
        //    return ValidateResponse(result);
        //}

        /// <summary>
        /// Obtiene los productos elegibles para beca de la persona autenticada:
        /// combina inscripciones realizadas, pendientes en workflow e intereses activos.
        /// Un registro por producto (el más antiguo por fecha de inscripción).
        /// </summary>
        /// <returns>Lista de productos beca.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        //[HttpGet("ProductosBeca")]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoBeca>>), 200)]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoBeca>>), 400)]
        //public IActionResult ObtenerProductosBeca()
        //{
        //    var result = catalogosService.ObtenerProductosBeca(_currentUser.GetUserId());
        //    return ValidateResponse(result);
        //}

        /// <summary>
        /// Obtiene los fondos de beca disponibles según el nivel de un producto.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <returns>Lista de tipos de descuento (fondos de beca).</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        //[HttpGet("FondosDeBecaPorProducto")]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTipoDescuentoDevart>>), 200)]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTipoDescuentoDevart>>), 400)]
        //public IActionResult ObtenerFondosDeBecaPorProducto([FromQuery] long idProducto)
        //{
        //    var result = catalogosService.ObtenerFondosDeBecaPorProducto(idProducto);
        //    return ValidateResponse(result);
        //}

        #endregion
    }
}
