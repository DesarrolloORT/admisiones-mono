using AppLogic.Enrollments.Survey.Dtos;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security.Authentication;

namespace WebApiAdmisiones.Controllers
{
    [Authorize]
    [ApiController]
    [Route("enrollments")]
    public class EnrollmentsController(
        IRegisterProductInterest registerProductInterest,
        IConfirmPreEnrollment confirmPreEnrollment,
        IReactivateEnrollment reactivateEnrollment,
        IGetEnrollmentDetails getEnrollmentDetails,
        IGetStudentRegulationsAcceptance getStudentRegulationsAcceptance,
        IStartEnrollmentPayment startEnrollmentPayment,
        IInitialSurveyService encuestaInicialService,
        ILogger<EnrollmentsController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<EnrollmentsController>(logger, currentUser)
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
        [HttpPost("product-interest")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        [ProducesResponseType(typeof(OperationResult<bool>), 409)]
        public IActionResult RegisterProductInterestRecord([FromBody] ProductInterestRequest request)
        {
            var result = registerProductInterest.Execute(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los datos de preinscripción (encuesta inicial) de la persona autenticada.
        /// </summary>
        /// <returns>Datos de preinscripción.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="404">No se encontraron datos de preinscripción para la persona.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("initial-survey")]
        [ProducesResponseType(typeof(OperationResult<GetInitialSurveyResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<GetInitialSurveyResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<GetInitialSurveyResponse>), 404)]
        public IActionResult GetInitialSurvey()
        {
            var result = encuestaInicialService.GetInitialSurvey(_currentUser.GetUserId());
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
        [HttpPost("initial-survey")]
        [ProducesResponseType(typeof(OperationResult<SaveInitialSurveyResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<SaveInitialSurveyResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<SaveInitialSurveyResponse>), 404)]
        public IActionResult SaveInitialSurvey([FromBody] SaveInitialSurveyRequest request)
        {
            var result = encuestaInicialService.SaveInitialSurvey(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Confirma la preinscripcion de la persona autenticada como cierre del paso 2.
        /// Valida encuesta definitiva, documentos frente y dorso, aceptacion del reglamento y confirma contra la API interna.
        /// Para productos de nivel 1 y 2 debe indicarse una unica oferta; para nivel 3 y 4 pueden indicarse varias.
        /// </summary>
        /// <param name="request">Ofertas seleccionadas y aceptacion del reglamento.</param>
        /// <returns>Confirmacion, resumen de carrera/comienzo/turno y un resultado (id de inscripcion, sena, vencimiento de pago) por cada oferta.</returns>
        /// <response code="200">Preinscripcion confirmada correctamente.</response>
        /// <response code="400">Solicitud invalida o datos incompletos para confirmar.</response>
        /// <response code="404">No se encontro la persona, encuesta o documento requerido.</response>
        /// <response code="409">La encuesta o el documento no estan vigentes o en estado valido.</response>
        [HttpPost("confirm-pre-enrollment")]
        [ProducesResponseType(typeof(OperationResult<ConfirmPreEnrollmentResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<ConfirmPreEnrollmentResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<ConfirmPreEnrollmentResponse>), 404)]
        [ProducesResponseType(typeof(OperationResult<ConfirmPreEnrollmentResponse>), 409)]
        public async Task<IActionResult> ConfirmPreEnrollment([FromBody] ConfirmPreEnrollmentRequest request)
        {
            var result = await confirmPreEnrollment.ExecuteAsync(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Reactiva una inscripcion dada de baja de la persona autenticada, creando una nueva
        /// inscripcion para la misma oferta.
        /// </summary>
        /// <param name="request">Id de la inscripcion dada de baja a reactivar.</param>
        /// <returns>Confirmacion, id de inscripcion, sena, vencimiento de pago y resumen de carrera, comienzo y turno.</returns>
        /// <response code="200">Inscripcion reactivada correctamente.</response>
        /// <response code="400">Solicitud invalida.</response>
        /// <response code="404">No se encontro la inscripcion para la persona.</response>
        /// <response code="409">La inscripcion indicada no esta dada de baja, o no esta en un estado valido para reactivar.</response>
        [HttpPost("reactivate")]
        [ProducesResponseType(typeof(OperationResult<ConfirmPreEnrollmentResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<ConfirmPreEnrollmentResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<ConfirmPreEnrollmentResponse>), 404)]
        [ProducesResponseType(typeof(OperationResult<ConfirmPreEnrollmentResponse>), 409)]
        public async Task<IActionResult> ReactivateEnrollment([FromBody] ReactivateEnrollmentRequest request)
        {
            var result = await reactivateEnrollment.ExecuteAsync(_currentUser.GetUserId(), request);
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
        [HttpPost("start-payment")]
        [ProducesResponseType(typeof(OperationResult<StartPaymentResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<StartPaymentResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<StartPaymentResponse>), 404)]
        [ProducesResponseType(typeof(OperationResult<StartPaymentResponse>), 409)]
        public async Task<IActionResult> StartPayment([FromBody] StartPaymentRequest request)
        {
            var result = await startEnrollmentPayment.ExecuteAsync(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Indica si la persona autenticada ya aceptó el reglamento estudiantil.
        /// </summary>
        /// <returns>Estado de aceptación del reglamento y fecha de primera aceptación.</returns>
        /// <response code="200">Consulta realizada correctamente.</response>
        [HttpGet("student-regulations")]
        [ProducesResponseType(typeof(OperationResult<StudentRegulationsAcceptanceResponse>), 200)]
        public IActionResult GetStudentRegulationsAcceptance()
        {
            var result = getStudentRegulationsAcceptance.Execute(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene el detalle de una inscripción de "Mis carreras" según su estado.
        /// </summary>
        /// <param name="productId">ID del producto de la tarjeta.</param>
        /// <param name="admissionProcessId">ID del proceso de la tarjeta.</param>
        /// <returns>Estado de la inscripción y, si corresponde, la oferta seleccionada.</returns>
        /// <response code="200">Detalle obtenido correctamente.</response>
        /// <response code="404">No se encontró la inscripción para la persona.</response>
        [HttpGet("details")]
        [ProducesResponseType(typeof(OperationResult<EnrollmentDetailsResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<EnrollmentDetailsResponse>), 404)]
        public async Task<IActionResult> GetEnrollmentDetails([FromQuery] long productId, [FromQuery] long admissionProcessId)
        {
            var result = await getEnrollmentDetails.ExecuteAsync(_currentUser.GetUserId(), productId, admissionProcessId);
            return ValidateResponse(result);
        }

        #endregion
    }
}
