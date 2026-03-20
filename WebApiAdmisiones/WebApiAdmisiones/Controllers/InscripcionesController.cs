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
    public class InscripcionesController(
        IInscripcionesService inscripcionesService,
        ILogger<InscripcionesController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<InscripcionesController>(logger, currentUser)
    {
        #region INSCRIPCIONES

        /// <summary>
        /// Obtiene la última inscripción de la persona autenticada.
        /// </summary>
        /// <returns>Última inscripción del alumno.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("UltimaInscripcionActiva")]
        [ProducesResponseType(typeof(OperationResult<DTOUltimaInscripcion>), 200)]
        [ProducesResponseType(typeof(OperationResult<DTOUltimaInscripcion>), 204)]
        [ProducesResponseType(typeof(OperationResult<DTOUltimaInscripcion>), 400)]
        public IActionResult ObtenerUltimaInscripcionActiva()
        {
            var result = inscripcionesService.ObtenerUltimaInscripcionActiva(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los productos vigentes con oferta abierta donde la persona tiene interés registrado y no está inscripta.
        /// </summary>
        /// <returns>Lista de productos vigentes con interés.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("ProductosVigentesConInteres")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 204)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 400)]
        public IActionResult ObtenerProductosVigentesConInteres()
        {
            var result = inscripcionesService.ObtenerProductosVigentesConInteres(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los productos de interés de la persona autenticada que aún no tienen inscripción confirmada.
        /// </summary>
        /// <returns>Lista de productos de interés.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("ProductosConInteresActivo")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 204)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOProductoAdmisiones>>), 400)]
        public IActionResult ObtenerProductosConInteresActivo()
        {
            var result = inscripcionesService.ObtenerProductosConInteresActivo(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Indica si la persona autenticada tiene una inscripción activa en VD_ES_FRESCO_ADMISION
        /// para el producto y proceso dados.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <param name="idProceso">ID del proceso.</param>
        /// <returns>true si existe inscripción activa; false en caso contrario.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("InscripcionActivaParaProceso")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        public IActionResult TieneInscripcionActivaParaProceso([FromQuery] long idProducto, [FromQuery] long idProceso)
        {
            var result = inscripcionesService.TieneInscripcionActivaParaProceso(_currentUser.GetUserId(), idProducto, idProceso);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las inscripciones en curso (workflow sin finalizar ni cancelar) de la persona autenticada.
        /// </summary>
        /// <returns>Lista de instancias de workflow pendientes, cada una con sus datos de inscripción.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("InscripcionesPendientes")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>), 400)]
        public IActionResult ObtenerInscripcionesPendientes()
        {
            var result = inscripcionesService.ObtenerInscripcionesPendientes(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las inscripciones canceladas de la persona autenticada.
        /// </summary>
        /// <returns>Lista de instancias de workflow canceladas, cada una con sus datos de inscripción.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("InscripcionesCanceladas")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoInstanciaWorkflowDevart>>), 400)]
        public IActionResult ObtenerInscripcionesCanceladas()
        {
            var result = inscripcionesService.ObtenerInscripcionesCanceladas(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene el historial de inscripciones realizadas por la persona autenticada,
        /// en productos de nivel 1 o 2 con proceso habilitado. Una entrada por producto (la más antigua).
        /// </summary>
        /// <returns>Lista de inscripciones realizadas.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("InscripcionesRealizadas")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOInscripcionRealizada>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DTOInscripcionRealizada>>), 400)]
        public IActionResult ObtenerInscripcionesRealizadas()
        {
            var result = inscripcionesService.ObtenerInscripcionesRealizadas(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Indica si la persona autenticada tiene una inscripción en T_INSCRIPTO (sin baja)
        /// para el producto y proceso dados.
        /// </summary>
        /// <param name="idProducto">ID del producto.</param>
        /// <param name="idProceso">ID del proceso.</param>
        /// <returns>true si existe la inscripción; false en caso contrario.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("InscripcionPorProductoProceso")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        public IActionResult TieneInscripcionAdmisiones([FromQuery] long idProducto, [FromQuery] long idProceso)
        {
            var result = inscripcionesService.TieneInscripcionAdmisiones(_currentUser.GetUserId(), idProducto, idProceso);
            return ValidateResponse(result);
        }

        #endregion
    }
}
