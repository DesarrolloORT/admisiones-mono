using BusinessLogic.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using AppLogic.Interfaces;
using AppLogic.Services;
using MailORT;
using AppLogic.DevartDTOs;
using ModBandejaAppLogic.DevartDTOs;
using AppLogic.Helpers;
using WebApiAdmisiones.Security;
using Utilities;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;

namespace WebApiAdmisiones.Controllers
{
    /// <summary>
    /// Controlador para la gestión de la Ficha de Persona (FDP).
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
        /// <param name="FdpService">Servicio de FDP.</param>
        /// <param name="logger">Logger para FdpController.</param>
        /// <param name="envioMailORT">Servicio de envío de mails.</param>
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
        /// Obtiene el país y sus ciudades asociadas.
        /// </summary>
        /// <param name="id">ID del país.</param>
        /// <returns>País y ciudades.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Pais")]
        [ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoPaisDevart>), 400)]
        public IActionResult ObtenerPais(long id)
        {
            var result = _GeneralService.ObtenerPais(id);
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
            var result = _GeneralService.ObtenerPaises();
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
            var result = _GeneralService.ObtenerTipoDocumentos();
            return ValidateResponse(result);
        }

        #endregion CONSULTAS(GET) GENERALES

        #region INTERES, PRODUCTOS, PROCESOS HABILITADOS

        /// <summary>
        /// Obtiene los procesos habilitados para un producto específico.
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
        /// Obtiene la última inscripción de la persona autenticada.
        /// </summary>
        /// <returns>Última inscripción del alumno.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("UltimaInscripcion")]
        [ProducesResponseType(typeof(OperationResult<DtoInscriptoDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoInscriptoDevart>), 204)]
        [ProducesResponseType(typeof(OperationResult<DtoInscriptoDevart>), 400)]
        public IActionResult ObtenerUltimaInscripcion()
        {
            var result = _GeneralService.ObtenerUltimaInscripcion(287023);
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
        /// Obtiene los datos de pre-inscripción (encuesta inicial) de la persona autenticada.
        /// </summary>
        /// <returns>Datos de pre-inscripción.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("DatosPreInscripcion")]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 204)]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 400)]
        public IActionResult ObtenerDatosPreInscripcion()
        {
            var result = _GeneralService.ObtenerDatosPreInscripcion(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los turnos disponibles para un producto y proceso de admisión.
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
            var result = _GeneralService.ObtenerMotivosEleccion();
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
            var result = _GeneralService.ObtenerPublicidadesEleccion();
            return ValidateResponse(result);
        }

        #endregion ENCUESTA

        #region BACHILLERATOS Y UNIVERSIDADES

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
            var result = _GeneralService.ObtenerBachilleratos(idAnioBachillerato);
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
            var result = _GeneralService.ObtenerAnioBachiller(idAnioBachillerato);
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
        /// Obtiene los fondos de beca disponibles según el nivel de un producto.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <returns>Lista de tipos de descuento (fondos de beca).</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("FondosDeBecaPorNivel")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTipoDescuentoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTipoDescuentoDevart>>), 204)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoTipoDescuentoDevart>>), 400)]
        public IActionResult ObtenerFondosDeBecaPorNivel([FromQuery] long idProducto)
        {
            var result = _GeneralService.ObtenerFondosDeBecaPorNivel(idProducto);
            return ValidateResponse(result);
        }

        #endregion POSTULACION A BECAS

        #region INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS

        /// <summary>
        /// Obtiene la aceptación del reglamento estudiantil de la persona autenticada.
        /// </summary>
        /// <returns>Datos de aceptación del reglamento.</returns>
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
        /// Obtiene los productos vigentes con oferta abierta donde la persona tiene interés registrado y no está inscripta.
        /// Equivalente al endpoint ProductosVigentesConInteres de la API anterior.
        /// </summary>
        /// <returns>Lista de productos vigentes con interés.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("ProductosVigentesConInteres")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoDevart>>), 204)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoDevart>>), 400)]
        public IActionResult ObtenerProductosVigentesConInteres()
        {
            var result = _GeneralService.ObtenerProductosVigentesConInteres(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los productos de interés de la persona autenticada que aún no tienen inscripción confirmada.
        /// </summary>
        /// <returns>Lista de productos de interés.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("ProductoInteresPersona")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoDevart>>), 204)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoDevart>>), 400)]
        public IActionResult ObtenerProductoInteresPersona()
        {
            var result = _GeneralService.ObtenerProductoInteresPersona(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        #endregion INSCRIPCION DE ALUMNOS FRESCOS A PRODUCTOS

    }
}
