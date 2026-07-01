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

        /// <summary>
        /// Registra o actualiza el interes de la persona autenticada para un producto, proceso y oferta habilitados.
        /// </summary>
        /// <param name="request">Producto, proceso y oferta seleccionados.</param>
        /// <returns><c>true</c> si el interes se registro correctamente.</returns>
        /// <response code="200">Interes registrado correctamente.</response>
        /// <response code="400">Producto, proceso, oferta o solicitud invalida.</response>
        /// <response code="404">Persona no encontrada.</response>
        /// <response code="409">La persona ya tuvo inscripcion o tiene una pendiente para ese producto.</response>
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
        [ProducesResponseType(typeof(OperationResult<DtoObtenerEncuestaInicialResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoObtenerEncuestaInicialResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoObtenerEncuestaInicialResponse>), 404)]
        public IActionResult ObtenerEncuestaInicialAdmision()
        {
            var result = inscripcionesService.ObtenerEncuestaInicial(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Guarda parcial o completamente la encuesta inicial de admision.
        /// </summary>
        /// <param name="request">Campos de encuesta enviados por el front.</param>
        /// <returns>Estado actualizado y campos pendientes de la encuesta.</returns>
        /// <response code="200">Encuesta guardada correctamente.</response>
        /// <response code="400">Los datos enviados son invalidos.</response>
        /// <response code="404">No se encontro la persona, producto o proceso indicado.</response>
        [HttpPost("EncuestaInicial")]
        [ProducesResponseType(typeof(OperationResult<DtoGuardarEncuestaInicialResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoGuardarEncuestaInicialResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoGuardarEncuestaInicialResponse>), 404)]
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

        /// <summary>
        /// Inicia o confirma el pago de una inscripcion de la persona autenticada.
        /// </summary>
        /// <remarks>
        /// Tipos de pago admitidos: <c>CUENTA_PERSONAL</c>, <c>ABITAB</c>, <c>PAGANZA</c>, <c>BANRED</c>, <c>GEOPAY</c> y <c>SISTARBANC</c>. Para <c>SISTARBANC</c> se debe enviar <c>IdBancoSistarbanc</c>.
        /// </remarks>
        /// <param name="request">Inscripcion y tipo de pago seleccionado por el front.</param>
        /// <returns>Resultado del pago, metodo guardado o URL generada para continuar el pago externo.</returns>
        /// <response code="200">Pago procesado, metodo guardado o URL de pago generada correctamente.</response>
        /// <response code="400">Solicitud invalida o tipo de pago no admitido.</response>
        /// <response code="404">No se encontro la inscripcion de la persona autenticada.</response>
        /// <response code="409">La inscripcion no esta en un estado valido para pagar.</response>
        [HttpPost("Pagar")]
        [ProducesResponseType(typeof(OperationResult<DtoPagarResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoPagarResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoPagarResponse>), 404)]
        [ProducesResponseType(typeof(OperationResult<DtoPagarResponse>), 409)]
        public async Task<IActionResult> Pagar([FromBody] DtoPagarRequest request)
        {
            var result = await inscripcionesService.Pagar(_currentUser.GetUserId(), request);
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
