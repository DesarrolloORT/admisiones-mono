using AppLogic.Dtos.EncuestaInicial;
using AppLogic.Dtos.Inscripciones;
using AppLogic.DevartDTOs;
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
        public IActionResult RegistrarInteresProducto([FromBody] DtoInteresProductoRequest request)
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
        public IActionResult GuardarEncuestaInicial([FromBody] DtoGuardarEncuestaInicialRequest request)
        {
            var result = inscripcionesService.GuardarEncuestaInicial(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Confirma la preinscripcion de la persona autenticada como cierre del paso 2.
        /// Valida encuesta definitiva, documentos frente y dorso, aceptacion del reglamento y confirma contra la API interna.
        /// </summary>
        /// <param name="request">Oferta seleccionada y aceptacion del reglamento.</param>
        /// <returns>Confirmacion, id de inscripcion, sena, vencimiento de pago y resumen de carrera, comienzo y turno.</returns>
        /// <response code="200">Preinscripcion confirmada correctamente.</response>
        /// <response code="400">Solicitud invalida o datos incompletos para confirmar.</response>
        /// <response code="404">No se encontro la persona, encuesta o documento requerido.</response>
        /// <response code="409">La encuesta o el documento no estan vigentes o en estado valido.</response>
        [HttpPost("ConfirmarPreInscripcion")]
        [ProducesResponseType(typeof(OperationResult<DtoConfirmarPreInscripcionResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoConfirmarPreInscripcionResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoConfirmarPreInscripcionResponse>), 404)]
        [ProducesResponseType(typeof(OperationResult<DtoConfirmarPreInscripcionResponse>), 409)]
        public async Task<IActionResult> ConfirmarPreInscripcion([FromBody] DtoConfirmarPreInscripcionRequest request)
        {
            var result = await inscripcionesService.ConfirmarPreInscripcion(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        [HttpPost("UrlFactura")]
        [ProducesResponseType(typeof(OperationResult<string>), 200)]
        [ProducesResponseType(typeof(OperationResult<string>), 400)]
        [ProducesResponseType(typeof(OperationResult<string>), 404)]
        public async Task<IActionResult> ObtenerUrlFactura([FromBody] DtoObtenerUrlFacturaRequest request)
        {
            var result = await inscripcionesService.ObtenerUrlFactura(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        [HttpPost("MetodoPago")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        [ProducesResponseType(typeof(OperationResult<bool>), 409)]
        public IActionResult GuardarMetodoPago([FromBody] DtoGuardarMetodoPagoRequest request)
        {
            var result = inscripcionesService.GuardarMetodoPago(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Indica si la persona autenticada ya aceptó el reglamento estudiantil.
        /// </summary>
        /// <returns>Estado de aceptación del reglamento y fecha de primera aceptación.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        [HttpGet("ReglamentoEstudiantil")]
        [ProducesResponseType(typeof(OperationResult<DtoAceptacionReglamentoEstudiantilResponse>), 200)]
        public IActionResult ObtenerAceptacionReglamentoEstudiantil()
        {
            var result = inscripcionesService.ObtenerAceptacionReglamentoEstudiantil(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene el detalle de una inscripción de "Mis carreras" según su estado.
        /// </summary>
        /// <param name="idProducto">ID del producto de la tarjeta.</param>
        /// <param name="idProceso">ID del proceso de la tarjeta.</param>
        /// <returns>Estado de la inscripción y, si corresponde, la oferta seleccionada.</returns>
        /// <response code="200">Detalle obtenido correctamente.</response>
        /// <response code="404">No se encontró la inscripción para la persona.</response>
        [HttpGet("Detalle")]
        [ProducesResponseType(typeof(OperationResult<DtoDetalleInscripcionResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoDetalleInscripcionResponse>), 404)]
        public async Task<IActionResult> ObtenerDetalleInscripcion([FromQuery] long idProducto, [FromQuery] long idProceso)
        {
            var result = await inscripcionesService.ObtenerDetalleInscripcion(_currentUser.GetUserId(), idProducto, idProceso);
            return ValidateResponse(result);
        }

        #endregion
    }
}
