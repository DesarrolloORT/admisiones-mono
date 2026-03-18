using BusinessLogic.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppLogic.Interfaces;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using WebApiAdmisiones.Security;
using Utilities;
using WebApiAdmisiones.Helpers;
using WebApiAdmisiones.Models;

namespace WebApiAdmisiones.Controllers
{
    /// <summary>
    /// Controlador para la gestiÃƒÂ³n de la Ficha de Persona (FDP).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class GeneralController : ApiBaseController<GeneralController>
    {
        #region PROPS & CONSTRUCTOR

        private readonly IGeneralServices _GeneralService;

        /// <summary>
        /// Constructor del controlador FDP.
        /// En este punto validamos si existe un usuario y capturamos su UserId.
        /// </summary>
        /// <param name="AdmisionesService">Servicio de admisiones.</param>
        /// <param name="logger">Logger para GeneralController.</param>
        /// <param name="currentUser">Servicio que expone el usuario actual.</param>
        public GeneralController(
            IGeneralServices AdmisionesService,
            ILogger<GeneralController> logger,
            ICurrentUserService currentUser) : base(logger, currentUser)
        {
            _GeneralService = AdmisionesService;
        }

        #endregion PROPS & CONSTRUCTOR

        #region CONSULTAS(GET) GENERALES

        /// <summary>
        /// Obtiene el paÃƒÂ­s y sus ciudades asociadas.
        /// </summary>
        /// <param name="id">ID del paÃƒÂ­s.</param>
        /// <returns>PaÃƒÂ­s y ciudades.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Pais")]
        [ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 400)]
        public IActionResult ObtenerPais([FromQuery] long id)
        {
            var result = _GeneralService.ObtenerPais(id);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene la lista de paÃƒÂ­ses.
        /// </summary>
        /// <returns>Lista de paÃƒÂ­ses.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Paises")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 400)]
        public IActionResult ObtenerPaises()
        {
            var result = _GeneralService.ObtenerPaises();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene el listado de tipos de documento disponibles.
        /// </summary>
        /// <returns>OperationResult con colecciÃƒÂ³n de DtoAcaTipoDocumentoDevart.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("TipoDocumentos")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 400)]
        public IActionResult ObtenerTipoDocumentos()
        {
            var result = _GeneralService.ObtenerTipoDocumentos();
            return ValidateResponse(result);
        }

        #endregion CONSULTAS(GET) GENERALES

        #region INTERES, PRODUCTOS, PROCESOS HABILITADOS

        /// <summary>
        /// Obtiene los procesos habilitados para un producto especÃƒÂ­fico.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <returns>Lista de procesos habilitados.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("ProcesosHabilitadosPorProducto")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProcesoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProcesoDevart>>), 204)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProcesoDevart>>), 400)]
        public IActionResult ObtenerProcesosHabilitadosPorProducto([FromQuery] long idProducto)
        {
            var result = _GeneralService.ObtenerProcesosHabilitadosPorProducto(idProducto);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene la ÃƒÂºltima inscripciÃƒÂ³n de la persona autenticada.
        /// </summary>
        /// <returns>ÃƒÅ¡ltima inscripciÃƒÂ³n del alumno.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("UltimaInscripcionActiva")]
        [ProducesResponseType(typeof(OperationResult<DTOUltimaInscripcion>), 200)]
        [ProducesResponseType(typeof(OperationResult<DTOUltimaInscripcion>), 204)]
        [ProducesResponseType(typeof(OperationResult<DTOUltimaInscripcion>), 400)]
        public IActionResult ObtenerUltimaInscripcionActiva()
        {
            var result = _GeneralService.ObtenerUltimaInscripcionActiva(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        #endregion INTERES, PRODUCTOS, PROCESOS HABILITADOS

        #region PERSONA

        /// <summary>
        /// Obtiene los datos de la persona autenticada.
        /// </summary>
        /// <returns>Datos de la persona.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Persona")]
        [ProducesResponseType(typeof(OperationResult<DtoPersonaDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoPersonaDevart>), 204)]
        [ProducesResponseType(typeof(OperationResult<DtoPersonaDevart>), 400)]
        public IActionResult ObtenerPersona()
        {
            var result = _GeneralService.ObtenerPersona(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        #endregion PERSONA

        #region ENCUESTA

        /// <summary>
        /// Obtiene los datos de pre-inscripciÃƒÂ³n (encuesta inicial) de la persona autenticada.
        /// </summary>
        /// <returns>Datos de pre-inscripciÃƒÂ³n.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("EncuestaInicialAdmision")]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 204)]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 400)]
        public IActionResult ObtenerEncuestaInicialAdmision()
        {
            var result = _GeneralService.ObtenerEncuestaInicialAdmision(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los turnos disponibles para un producto y proceso de admisiÃƒÂ³n.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <param name="idProceso">ID del proceso.</param>
        /// <returns>Lista de turnos disponibles.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Turnos")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTurnoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTurnoDevart>>), 204)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTurnoDevart>>), 400)]
        public IActionResult ObtenerTurnos([FromQuery] long idProducto, [FromQuery] long idProceso)
        {
            var result = _GeneralService.ObtenerTurnos(idProducto, idProceso);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los motivos de elecciÃƒÂ³n disponibles para la encuesta de admisiÃƒÂ³n.
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
            var result = _GeneralService.ObtenerMotivosEleccion();
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las publicidades de elecciÃƒÂ³n disponibles para la encuesta de admisiÃƒÂ³n.
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
            var result = _GeneralService.ObtenerPublicidadesEleccion();
            return ValidateResponse(result);
        }

        #endregion ENCUESTA

        #region BACHILLERATOS Y UNIVERSIDADES

        /// <summary>
        /// Obtiene los bachilleratos para un aÃƒÂ±o de bachiller dado.
        /// </summary>
        /// <param name="idAnioBachillerato">ID del aÃƒÂ±o de bachillerato.</param>
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
            var result = _GeneralService.ObtenerBachilleratos(idAnioBachillerato);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los datos de un aÃƒÂ±o de bachiller por su ID.
        /// </summary>
        /// <param name="idAnioBachillerato">ID del aÃƒÂ±o de bachillerato.</param>
        /// <returns>Datos del aÃƒÂ±o de bachiller.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("AnioBachiller")]
        [ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 204)]
        [ProducesResponseType(typeof(OperationResult<DtoAnioBachillerDevart>), 400)]
        public IActionResult ObtenerAnioBachiller([FromQuery] long idAnioBachillerato)
        {
            var result = _GeneralService.ObtenerAnioBachiller(idAnioBachillerato);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las instituciones educativas para un paÃƒÂ­s y estado dados.
        /// </summary>
        /// <param name="codigoPais">CÃƒÂ³digo del paÃƒÂ­s.</param>
        /// <param name="codigoEstado">CÃƒÂ³digo del estado/departamento.</param>
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
            var result = _GeneralService.ObtenerInstituciones(codigoPais, codigoEstado);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las universidades disponibles para pre-grado.
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
            var result = _GeneralService.ObtenerUniversidades();
            return ValidateResponse(result);
        }

        #endregion BACHILLERATOS Y UNIVERSIDADES

        #region POSTULACION A BECAS

        /// <summary>
        /// Obtiene los fondos de beca disponibles segÃƒÂºn el nivel de un producto.
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
            var result = _GeneralService.ObtenerFondosDeBecaPorProducto(idProducto);
            return ValidateResponse(result);
        }

        #endregion POSTULACION A BECAS

        #region INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS

        /// <summary>
        /// Obtiene la aceptaciÃƒÂ³n del reglamento estudiantil de la persona autenticada.
        /// </summary>
        /// <returns>Datos de aceptaciÃƒÂ³n del reglamento.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("AceptacionReglamentoEstudiantil")]
        [ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 204)]
        [ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 400)]
        public IActionResult ObtenerAceptacionReglamentoEstudiantil()
        {
            var result = _GeneralService.ObtenerAceptacionReglamentoEstudiantil(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los productos vigentes con oferta abierta donde la persona tiene interÃƒÂ©s registrado y no estÃƒÂ¡ inscripta.
        /// </summary>
        /// <returns>Lista de productos vigentes con interÃƒÂ©s.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("ProductosVigentesConInteres")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 204)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 400)]
        public IActionResult ObtenerProductosVigentesConInteres()
        {
            var result = _GeneralService.ObtenerProductosVigentesConInteres(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los productos de interÃƒÂ©s de la persona autenticada que aÃƒÂºn no tienen inscripciÃƒÂ³n confirmada.
        /// </summary>
        /// <returns>Lista de productos de interÃƒÂ©s.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("ProductosConInteresActivo")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 204)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 400)]
        public IActionResult ObtenerProductosConInteresActivo()
        {
            var result = _GeneralService.ObtenerProductosConInteresActivo(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        #endregion INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS

        #region INSCRIPCION Ã¢â‚¬â€ WORKFLOW

        /// <summary>
        /// Indica si la persona autenticada tiene una inscripciÃƒÂ³n activa en VD_ES_FRESCO_ADMISION
        /// para el producto y proceso dados.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <param name="idProceso">ID del proceso.</param>
        /// <returns>true si existe inscripciÃƒÂ³n activa, false en caso contrario.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("InscripcionActivaParaProceso")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        public IActionResult TieneInscripcionActivaParaProceso([FromQuery] long idProducto, [FromQuery] long idProceso)
        {
            var result = _GeneralService.TieneInscripcionActivaParaProceso(_currentUser.GetUserId(), idProducto, idProceso);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las inscripciones en curso (workflow sin finalizar ni cancelar) de la persona autenticada.
        /// </summary>
        /// <returns>Lista de instancias de workflow pendientes, cada una con sus datos de inscripciÃƒÂ³n.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("InscripcionesPendientes")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>), 400)]
        public IActionResult ObtenerInscripcionesPendientes()
        {
            var result = _GeneralService.ObtenerInscripcionesPendientes(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las inscripciones canceladas de la persona autenticada.
        /// </summary>
        /// <returns>Lista de instancias de workflow canceladas, cada una con sus datos de inscripciÃƒÂ³n.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("InscripcionesCanceladas")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>), 400)]
        public IActionResult ObtenerInscripcionesCanceladas()
        {
            var result = _GeneralService.ObtenerInscripcionesCanceladas(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las ofertas disponibles para inscripciÃƒÂ³n de alumno fresco,
        /// dado un producto, proceso y turno.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <param name="idProceso">ID del proceso.</param>
        /// <param name="idTurno">ID del turno.</param>
        /// <returns>Lista de ofertas disponibles.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("OfertasParaInscripcionConProceso")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoOfertaDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoOfertaDevart>>), 400)]
        public IActionResult ObtenerOfertasParaInscripcionConProceso([FromQuery] long idProducto, [FromQuery] long idProceso, [FromQuery] long idTurno)
        {
            var result = _GeneralService.ObtenerOfertasParaInscripcionConProceso(idProducto, idProceso, idTurno);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene el historial de inscripciones realizadas por la persona autenticada,
        /// en productos de nivel 1 o 2 con proceso habilitado. Una entrada por producto (la mÃƒÂ¡s antigua).
        /// </summary>
        /// <returns>Lista de inscripciones realizadas.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("InscripcionesRealizadas")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOInscripcionRealizada>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOInscripcionRealizada>>), 400)]
        public IActionResult ObtenerInscripcionesRealizadas()
        {
            var result = _GeneralService.ObtenerInscripcionesRealizadas(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los productos elegibles para beca de la persona autenticada:
        /// combina inscripciones realizadas, pendientes en workflow e intereses activos.
        /// Un registro por producto (el mÃƒÂ¡s antiguo por fecha de inscripciÃƒÂ³n).
        /// </summary>
        /// <returns>Lista de productos beca.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("ProductosBeca")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoBeca>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoBeca>>), 400)]
        public IActionResult ObtenerProductosBeca()
        {
            var result = _GeneralService.ObtenerProductosBeca(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Indica si la persona autenticada tiene una inscripciÃƒÂ³n en T_INSCRIPTO (sin baja)
        /// para el producto y proceso dados.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <param name="idProceso">ID del proceso.</param>
        /// <returns>true si existe la inscripciÃƒÂ³n, false en caso contrario.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("InscripcionPorProductoProceso")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        public IActionResult TieneInscripcionAdmisiones([FromQuery] long idProducto, [FromQuery] long idProceso)
        {
            var result = _GeneralService.TieneInscripcionAdmisiones(_currentUser.GetUserId(), idProducto, idProceso);
            return ValidateResponse(result);
        }

        #endregion INSCRIPCION Ã¢â‚¬â€ WORKFLOW

        #region IMAGEN / DOCUMENTOS

        /// <summary>
        /// Obtiene la foto de perfil del alumno autenticado.
        /// </summary>
        /// <returns>Imagen JPEG de la foto.</returns>
        /// <response code="200">Foto obtenida correctamente.</response>
        /// <response code="404">Foto no encontrada o sin imagen.</response>
        [HttpGet("FotoAlumno")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(OperationResult<byte[]>), 404)]
        public IActionResult ObtenerFotoAlumno()
        {
            var result = _GeneralService.ObtenerFotoAlumno(_currentUser.GetUserId());

            if (!result.Success)
                return ValidateResponse(result);

            if (result.Data is null)
                return NotFound();

            return File(result.Data, "image/jpeg");
        }

        /// <summary>
        /// Obtiene el documento de identidad (cÃƒÂ©dula) del alumno autenticado.
        /// </summary>
        /// <param name="tipo">Cara del documento: 1 = frente, 2 = dorso.</param>
        /// <returns>Imagen JPEG del documento.</returns>
        /// <response code="200">Imagen obtenida correctamente.</response>
        /// <response code="204">El documento estÃƒÂ¡ vencido.</response>
        /// <response code="400">Tipo de documento invÃƒÂ¡lido.</response>
        /// <response code="404">Documento no encontrado o sin imagen.</response>
        [HttpGet("DocumentoAlumno")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(OperationResult<byte[]>), 204)]
        [ProducesResponseType(typeof(OperationResult<byte[]>), 400)]
        [ProducesResponseType(typeof(OperationResult<byte[]>), 404)]
        public IActionResult ObtenerDocumentoAlumno([FromQuery] int tipo)
        {
            var result = _GeneralService.ObtenerDocumentoAlumno(_currentUser.GetUserId(), tipo);

            if (!result.Success)
                return ValidateResponse(result);

            if (result.Data is null)
                return NotFound();

            return File(result.Data, "image/jpeg");
        }

        /// <summary>
        /// Sube la foto del alumno autenticado.
        /// </summary>
        /// <param name="request">JSON con el nombre del archivo y los bytes de la imagen.</param>
        /// <returns>true si la foto se guardÃ³ correctamente.</returns>
        /// <response code="200">Archivo guardado correctamente.</response>
        /// <response code="400">Request inválido o archivo no permitido.</response>
        /// <response code="404">No se encontrÃ³ la persona autenticada.</response>
        [HttpPost("SubirFotoAlumno")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult SubirFotoAlumno([FromBody] UploadArchivoRequest request)
        {
            var fileContent = request.Archivo ?? Array.Empty<byte>();
            var fileName = string.IsNullOrWhiteSpace(request.NombreArchivo) ? "image.jpg" : request.NombreArchivo;
            var result = _GeneralService.SubirFotoAlumno(_currentUser.GetUserId(), fileContent, fileName);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Sube un documento del alumno autenticado para el tipo y fecha de vencimiento indicados.
        /// </summary>
        /// <param name="request">JSON con tipo, fecha de vencimiento, nombre del archivo y bytes del documento.</param>
        /// <returns>true si el documento se guardÃ³ correctamente.</returns>
        /// <response code="200">Archivo guardado correctamente.</response>
        /// <response code="400">Request inválido o archivo no permitido.</response>
        /// <response code="404">No se encontrÃ³ la persona autenticada.</response>
        [HttpPost("SubirDocumentoAlumno")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult SubirDocumentoAlumno([FromBody] UploadDocumentoAlumnoRequest request)
        {
            var fileContent = request.Archivo ?? Array.Empty<byte>();
            var fileName = request.NombreArchivo ?? string.Empty;
            var result = _GeneralService.SubirDocumentoAlumno(
                _currentUser.GetUserId(),
                request.Tipo,
                request.Fecha,
                fileContent,
                fileName);

            return ValidateResponse(result);
        }

        #endregion IMAGEN / DOCUMENTOS

        #region ADMISIONES

        /// <summary>
        /// Obtiene la fecha de vencimiento de admisiones para el alumno autenticado,
        /// calculada en base al proceso y los dÃƒÂ­as hÃƒÂ¡biles.
        /// </summary>
        /// <param name="idProceso">ID del proceso de admisiones seleccionado.</param>
        /// <returns>Fecha de vencimiento calculada.</returns>
        /// <response code="200">Fecha obtenida correctamente.</response>
        /// <response code="400">Proceso invÃƒÂ¡lido o sin fecha de comienzo.</response>
        [HttpGet("FechaVencimientoAdmisiones")]
        [ProducesResponseType(typeof(OperationResult<DateTime>), 200)]
        [ProducesResponseType(typeof(OperationResult<DateTime>), 400)]
        public IActionResult ObtenerFechaVencimientoAdmisiones([FromQuery] long idProceso)
        {
            var result = _GeneralService.ObtenerFechaVencimientoAdmisiones(_currentUser.GetUserId(), idProceso);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los fondos de beca vigentes para el alumno autenticado,
        /// dado un producto y proceso.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <param name="idProceso">ID del proceso.</param>
        /// <returns>Lista de pruebas de beca vigentes.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Producto invÃƒÂ¡lido.</response>
        [HttpGet("FondosDeBecaVigentes")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPruebaDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPruebaDevart>>), 400)]
        public IActionResult ObtenerFondosDeBecaVigentes([FromQuery] long idProducto, [FromQuery] long idProceso)
        {
            var result = _GeneralService.ObtenerFondosDeBecaVigentes(idProducto, idProceso, _currentUser.GetUserId());
            return ValidateResponse(result);
        }

        #endregion ADMISIONES
    }
}
