using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices.Inscripciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security.Authentication;

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

        ///// <summary>
        ///// Registra o actualiza el interés de la persona autenticada para un producto y proceso habilitado.
        ///// </summary>
        ///// <param name="request">Producto y proceso seleccionados.</param>
        ///// <returns>Resultado de la actualización del interés.</returns>
        ///// <response code="200">Interés registrado correctamente.</response>
        ///// <response code="400">Producto o proceso inválido.</response>
        ///// <response code="404">Persona no encontrada.</response>
        ///// <response code="409">La persona ya tuvo inscripción o tiene una pendiente para ese producto.</response>
        [HttpPost("InteresProducto")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        [ProducesResponseType(typeof(OperationResult<bool>), 409)]
        public IActionResult RegistrarInteresProducto([FromBody] InteresProductoRequest request)
        {
            var result = inscripcionesService.RegistrarInteresProducto(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los datos de preinscripción (encuesta inicial) de la persona autenticada.
        /// </summary>
        /// <returns>Datos de preinscripción.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="404">No se encontraron datos de preinscripción para la persona.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("EncuestaInicial")]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaInicialAdmisionResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaInicialAdmisionResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaInicialAdmisionResponse>), 404)]
        public IActionResult ObtenerEncuestaInicialAdmision()
        {
            var result = inscripcionesService.ObtenerEncuestaInicial(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Guarda parcial o completamente la encuesta inicial de admision.
        /// </summary>
        /// <param name="request">Campos de encuesta enviados por el front.</param>
        /// <returns><c>true</c> si la encuesta se guardo correctamente.</returns>
        /// <response code="200">Encuesta guardada correctamente.</response>
        /// <response code="400">Los datos enviados son invalidos.</response>
        /// <response code="404">No se encontro la persona, producto o proceso indicado.</response>
        [HttpPost("EncuestaInicial")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult GuardarEncuestaInicial([FromBody] GuardarEncuestaInicialRequest request)
        {
            var result = inscripcionesService.GuardarEncuestaInicial(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Registra la aceptación del reglamento estudiantil para la persona autenticada.
        /// Toma el producto y el comienzo desde la encuesta inicial de admisión vigente.
        /// </summary>
        /// <returns>Datos de la aceptación registrada.</returns>
        /// <response code="200">Aceptación registrada correctamente.</response>
        /// <response code="400">La encuesta no contiene producto o comienzo válidos.</response>
        /// <response code="404">No se encontró la persona o la encuesta inicial de admisión.</response>
        /// <response code="409">Ya existe una aceptación registrada para la persona, producto y comienzo.</response>
        [HttpPost("ReglamentoEstudiantil")]
        [ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 404)]
        [ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstDevart>), 409)]
        public IActionResult RegistrarAceptacionReglamentoEstudiantil()
        {
            var result = inscripcionesService.RegistrarAceptacionReglamentoEstudiantil(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        ///// <summary>
        ///// Obtiene la última inscripción activa de la persona autenticada.
        ///// </summary>
        ///// <returns>Última inscripción del alumno.</returns>
        ///// <response code="200">Datos obtenidos correctamente.</response>
        ///// <response code="404">No se encontró una inscripción activa para la persona.</response>
        ///// <response code="400">Solicitud inválida.</response>
        //[HttpGet("UltimaInscripcionActiva")]
        //[ProducesResponseType(typeof(OperationResult<DtoUltimaInscripcion>), 200)]
        //[ProducesResponseType(typeof(OperationResult<DtoUltimaInscripcion>), 400)]
        //[ProducesResponseType(typeof(OperationResult<DtoUltimaInscripcion>), 404)]
        //public IActionResult ObtenerUltimaInscripcionActiva()
        //{
        //    var result = inscripcionesService.ObtenerUltimaInscripcionActiva(_currentUser.GetUserId());
        //    return ValidateResponse(result);
        //}

        ///// <summary>
        ///// Obtiene los productos vigentes con oferta abierta donde la persona tiene interés registrado y no está inscripta.
        ///// </summary>
        ///// <returns>Lista de productos vigentes con interés.</returns>
        ///// <response code="200">Datos obtenidos correctamente.</response>
        ///// <response code="400">Solicitud inválida.</response>
        //[HttpGet("ProductosVigentesConInteres")]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoAdmisiones>>), 200)]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoAdmisiones>>), 400)]
        //public IActionResult ObtenerProductosVigentesConInteres()
        //{
        //    var result = inscripcionesService.ObtenerProductosVigentesConInteres(_currentUser.GetUserId());
        //    return ValidateResponse(result);
        //}

        ///// <summary>
        ///// Obtiene los productos de interés de la persona autenticada que aún no tienen inscripción confirmada.
        ///// </summary>
        ///// <returns>Lista de productos de interés.</returns>
        ///// <response code="200">Datos obtenidos correctamente.</response>
        ///// <response code="400">Solicitud inválida.</response>
        //[HttpGet("ProductosConInteresActivo")]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoAdmisiones>>), 200)]
        //[ProducesResponseType(typeof(OperationResult<IEnumerable<DtoProductoAdmisiones>>), 400)]
        //public IActionResult ObtenerProductosConInteresActivo()
        //{
        //    var result = inscripcionesService.ObtenerProductosConInteresActivo(_currentUser.GetUserId());
        //    return ValidateResponse(result);
        //}

        ///// <summary>
        ///// Indica si la persona autenticada tiene una inscripción activa en VD_ES_FRESCO_ADMISION
        ///// para el producto y proceso dados.
        ///// </summary>
        ///// <param name="idProducto">ID del producto.</param>
        ///// <param name="idProceso">ID del proceso.</param>
        ///// <returns><c>true</c> si existe inscripción activa; <c>false</c> en caso contrario.</returns>
        ///// <response code="200">Consulta realizada correctamente.</response>
        ///// <response code="400">Solicitud inválida.</response>
        //[HttpGet("InscripcionActivaParaProceso")]
        //[ProducesResponseType(typeof(OperationResult<bool>), 200)]
        //[ProducesResponseType(typeof(OperationResult<bool>), 400)]
        //public IActionResult TieneInscripcionActivaParaProceso([FromQuery] long idProducto, [FromQuery] long idProceso)
        //{
        //    var result = inscripcionesService.TieneInscripcionActivaParaProceso(_currentUser.GetUserId(), idProducto, idProceso);
        //    return ValidateResponse(result);
        //}

        ///// <summary>
        ///// Indica si la persona autenticada tiene una inscripción en T_INSCRIPTO (sin baja)
        ///// para el producto y proceso dados.
        ///// </summary>
        ///// <param name="idProducto">ID del producto.</param>
        ///// <param name="idProceso">ID del proceso.</param>
        ///// <returns><c>true</c> si existe la inscripción; <c>false</c> en caso contrario.</returns>
        ///// <response code="200">Consulta realizada correctamente.</response>
        ///// <response code="400">Solicitud inválida.</response>
        //[HttpGet("InscripcionPorProductoProceso")]
        //[ProducesResponseType(typeof(OperationResult<bool>), 200)]
        //[ProducesResponseType(typeof(OperationResult<bool>), 400)]
        //public IActionResult TieneInscripcionAdmisiones([FromQuery] long idProducto, [FromQuery] long idProceso)
        //{
        //    var result = inscripcionesService.TieneInscripcionAdmisiones(_currentUser.GetUserId(), idProducto, idProceso);
        //    return ValidateResponse(result);
        //}

        #endregion
    }
}
