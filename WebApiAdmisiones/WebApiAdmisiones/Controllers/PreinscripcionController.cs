using AppLogic.DevartDTOs;
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
    public class PreinscripcionController(
        IPreinscripcionService preinscripcionService,
        ILogger<PreinscripcionController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<PreinscripcionController>(logger, currentUser)
    {
        #region PREINSCRIPCION

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
            var result = preinscripcionService.ObtenerProcesosHabilitadosPorProducto(idProducto);
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
            var result = preinscripcionService.ObtenerTurnos(idProducto, idProceso);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las ofertas disponibles para inscripción de alumno fresco,
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
            var result = preinscripcionService.ObtenerOfertasParaInscripcionConProceso(idProducto, idProceso, idTurno);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene la fecha de vencimiento de admisiones para el alumno autenticado,
        /// calculada en base al proceso y los días hábiles.
        /// </summary>
        /// <param name="idProceso">ID del proceso de admisiones seleccionado.</param>
        /// <returns>Fecha de vencimiento calculada.</returns>
        /// <response code="200">Fecha obtenida correctamente.</response>
        /// <response code="400">Proceso inválido o sin fecha de comienzo.</response>
        [HttpGet("FechaVencimientoAdmisiones")]
        [ProducesResponseType(typeof(OperationResult<DateTime>), 200)]
        [ProducesResponseType(typeof(OperationResult<DateTime>), 400)]
        public IActionResult ObtenerFechaVencimientoAdmisiones([FromQuery] long idProceso)
        {
            var result = preinscripcionService.ObtenerFechaVencimientoAdmisiones(_currentUser.GetUserId(), idProceso);
            return ValidateResponse(result);
        }

        #endregion
    }
}
