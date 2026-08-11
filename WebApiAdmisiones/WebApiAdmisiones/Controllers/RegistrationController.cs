using AppLogic.Identity.Interfaces;
using AppLogic.Identity.Dtos;
using AppLogic.Registration.Dtos;
using AppLogic.Registration.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AzureService.DTOs;
using AzureService.Interfaces;
using WebApiAdmisiones.Models;
using Utilities;
using WebApiAdmisiones.Security.Authentication;
using WebApiAdmisiones.Security.Captcha;
using AppLogic.Contracts;
using AppLogic.Registration.Mapping;
using AppLogic.Registration.Contracts;
using AppLogic.Registration.Interfaces;

namespace WebApiAdmisiones.Controllers
{
    /// <summary>
    /// Endpoints públicos del flujo de registro previo a la autenticación.
    /// </summary>
    [ApiController]
    [Route("registration")]
    public class RegistrationController(
        IEvaluateDocument evaluateDocument,
        IVerifyIdentity verifyIdentity,
        IConfirmRegistrationRequest confirmRegistrationRequest,
        IRegistrationFlowService registroFlowService,
        IReconocimientoDocumento reconocimientoDocumentoService,
        IIdentityDocumentImageCache documentoImagenCacheService,
        ILogger<RegistrationController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<RegistrationController>(logger, currentUser)
    {
        private const string FlowIdHeaderName = "X-Flow-Id";

        /// <summary>
        /// Valida que la sesión de registro del flowId exista y esté en el step "evaluado".
        /// El flowId lo bindea MVC desde el header <c>X-Flow-Id</c> de cada acción.
        /// </summary>
        private async Task<IActionResult?> ValidarFlowEvaluadoAsync(string? flowId)
        {
            var flowValidation = await registroFlowService.ValidateFlowSessionAsync(flowId, stepEsperado: RegistrationFlowConstants.Step.Evaluado);
            return flowValidation != null ? ValidateResponse(flowValidation) : null;
        }

        /// <summary>
        /// Valida que el documento recibido coincida con el de la sesión de registro.
        /// </summary>
        private async Task<IActionResult?> ValidarDocumentoDeFlowAsync(
            string flowId, string? documentType, string? document, string originMethod)
        {
            var documentValidation = await registroFlowService.ValidateFlowDocumentAsync(
                flowId, documentType, document, originMethod);
            return documentValidation != null ? ValidateResponse(documentValidation) : null;
        }

        /// <summary>
        /// Evalua si el documento ingresado puede iniciar el registro.
        /// </summary>
        /// <remarks>
        /// Endpoint publico para el primer paso del onboarding. El front envia tipo y numero de documento y recibe el estado funcional para decidir si continua con una persona existente, una persona nueva o una solicitud pendiente.
        /// La respuesta incluye un <c>flowId</c> que debe enviarse en el header <c>X-Flow-Id</c> en los pasos siguientes.
        /// </remarks>
        /// <param name="request">Tipo y numero de documento que se quiere registrar.</param>
        /// <returns>Estado de evaluacion del documento para guiar el flujo de registro.</returns>
        /// <response code="200">Documento evaluado correctamente.</response>
        /// <response code="400">Datos invalidos o regla funcional no cumplida.</response>
        [AllowAnonymous]
        [RequireCaptcha(CaptchaActions.EvaluateDocument, CaptchaValidationMode.ScoreOnly)]
        [HttpPost("evaluate-document")]
        [ProducesResponseType(typeof(OperationResult<DocumentEvaluationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DocumentEvaluationResponse>), 400)]
        public async Task<IActionResult> EvaluateDocument([FromBody] EvaluateDocumentRequest request)
        {
            var result = await evaluateDocument.ExecuteAsync(request);

            if (result.Success && result.Data != null && !result.Data.UserAlreadyRegistered)
            {
                // Crear sesión de registro y adjuntar flowId a la respuesta
                var flowId = await registroFlowService.CreateFlowSessionAsync(
                    request?.DocumentType ?? string.Empty,
                    request?.DocumentNumber ?? string.Empty,
                    personId: null);

                result.Data.FlowId = flowId;
            }

            return ValidateResponse(result);
        }

        /// <summary>
        /// Verifica la identidad de una persona existente.
        /// </summary>
        /// <remarks>
        /// Endpoint publico para confirmar que quien continua el registro conoce los datos requeridos de la persona encontrada por documento.
        /// Si la identidad es valida, se crea el usuario LDAP y se envia el mail de activacion de password.
        /// Requiere el header <c>X-Flow-Id</c> obtenido de EvaluateDocument.
        /// </remarks>
        /// <param name="request">Datos de validacion de identidad asociados a la persona.</param>
        /// <param name="flowId">Identificador de la sesion de registro, enviado en el header <c>X-Flow-Id</c>.</param>
        /// <returns>Resultado de la verificacion de identidad.</returns>
        /// <response code="200">Identidad verificada correctamente.</response>
        /// <response code="400">Datos invalidos, verificacion rechazada o sesion de registro expirada.</response>
        [AllowAnonymous]
        [RequireCaptcha(CaptchaActions.VerifyIdentity, CaptchaValidationMode.ScoreOnly)]
        [HttpPost("verify-identity")]
        [ProducesResponseType(typeof(OperationResult<RegistrationConfirmationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<RegistrationConfirmationResponse>), 400)]
        public async Task<IActionResult> VerifyIdentity(
            [FromBody] VerifyIdentityRequest request,
            [FromHeader(Name = FlowIdHeaderName)] string? flowId = null)
        {
            var flowError = await ValidarFlowEvaluadoAsync(flowId);
            if (flowError != null) return flowError;

            if (request == null)
            {
                return ValidateResponse(OperationResult<RegistrationConfirmationResponse?>.IsFailed(
                    "REG_REQUEST_01",
                    nameof(VerifyIdentity),
                    "La solicitud es obligatoria.",
                    400));
            }

            var documentError = await ValidarDocumentoDeFlowAsync(
                flowId!, request.DocumentType, request.DocumentNumber, nameof(VerifyIdentity));
            if (documentError != null) return documentError;

            var result = await verifyIdentity.ExecuteAsync(request);

            if (result.Success && flowId != null)
            {
                await registroFlowService.UpdateStepAsync(flowId, RegistrationFlowConstants.Step.Confirmado);
            }

            return ValidateResponse(result);
        }

        /// <summary>
        /// Analiza una imagen o PDF de documento usando Azure Document Intelligence, detecta si se trata
        /// de cédula uruguaya, pasaporte o documento extranjero admitido, y devuelve los datos extraídos.
        /// </summary>
        /// <remarks>
        /// Este endpoint está protegido por rate limiting: máximo 5 solicitudes por minuto por usuario/IP.
        /// </remarks>
        /// <param name="request">Archivo adjunto y tipo MIME del documento a reconocer.</param>
        /// <returns>Datos extraidos del documento y resultado del analisis de identidad visual, cuando corresponda.</returns>
        /// <response code="200">Documento reconocido correctamente.</response>
        /// <response code="400">No se recibio archivo o la solicitud es invalida.</response>
        /// <response code="422">El archivo no cumple las reglas de validacion o no corresponde a un documento admitido.</response>
        /// <response code="429">Se supero el limite de solicitudes de reconocimiento.</response>
        /// <response code="500">Error interno al procesar el documento.</response>
        /// <response code="502">Error del proveedor externo de reconocimiento.</response>
        /// <response code="504">Timeout al consultar el proveedor externo de reconocimiento.</response>
        [AllowAnonymous]
        [EnableRateLimiting("ReconocimientoDocumento")]
        [RequireCaptcha(CaptchaActions.AnalyzeAttachment, CaptchaValidationMode.ScoreOnly)]
        [HttpPost("analyze-attachment")]
        [ProducesResponseType(typeof(OperationResult<DocumentRecognitionResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DocumentRecognitionResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DocumentRecognitionResponse>), 422)]
        [ProducesResponseType(typeof(OperationResult<DocumentRecognitionResponse>), 429)]
        [ProducesResponseType(typeof(OperationResult<DocumentRecognitionResponse>), 500)]
        [ProducesResponseType(typeof(OperationResult<DocumentRecognitionResponse>), 502)]
        [ProducesResponseType(typeof(OperationResult<DocumentRecognitionResponse>), 504)]
        public async Task<IActionResult> AnalyzeAttachment([FromBody] RecognizeDocumentApiRequest? request)
        {
            if (request?.File is null)
            {
                return ValidateResponse(OperationResult<DocumentRecognitionResponse>.IsFailed(
                    "REC_DOC_01",
                    nameof(AnalyzeAttachment),
                    "No se recibió la solicitud de reconocimiento.",
                    400,
                    default!));
            }

            var fileContent = request.File.Content ?? Array.Empty<byte>();
            var fileName = FileValidator.ResolveFileName(request.File.FileName, request.MimeType);

            var result = await reconocimientoDocumentoService.ReconocerDocumentoAsync(
                new ReconocimientoDocumentoRequest
                {
                    Archivo = fileContent,
                    NombreArchivo = fileName,
                    TipoMime = request.MimeType
                });

            if (result.Success && result.Data?.Campos is not null)
            {
                await TrySaveDocumentImagesAsync(
                    result.Data,
                    fileContent,
                    fileName,
                    request.MimeType);
            }

            return ValidateResponse(result.Success
                ? OperationResult<DocumentRecognitionResponse>.IsSuccess(result.Data!.ToResponse(), result.Method, result.Message)
                : result.Failure().As<DocumentRecognitionResponse>(nameof(AnalyzeAttachment)));
        }

        private async Task TrySaveDocumentImagesAsync(
            ReconocimientoDocumentoResponse reconocimiento,
            byte[] documentoOriginal,
            string nombreDocumento,
            string? tipoMime)
        {
            var documentFront = new TemporaryDocumentFile
            {
                Content = documentoOriginal,
                FileName = nombreDocumento,
                ContentType = string.IsNullOrWhiteSpace(tipoMime)
                    ? "application/octet-stream"
                    : tipoMime
            };

            var personFace = reconocimiento.CaraPersona is null
                ? null
                : new TemporaryDocumentFile
                {
                    Content = reconocimiento.CaraPersona.Archivo,
                    FileName = reconocimiento.CaraPersona.NombreArchivo,
                    ContentType = reconocimiento.CaraPersona.ContentType
                };

            await documentoImagenCacheService.SaveTemporaryImagesIfApplicableAsync(
                reconocimiento.Campos.TipoDocumento,
                reconocimiento.Campos.NumeroDocumento,
                reconocimiento.Campos.FechaVencimiento,
                documentFront,
                personFace);
        }

        /// <summary>
        /// Confirma el registro de una persona nueva.
        /// </summary>
        /// <remarks>
        /// Endpoint publico protegido por captcha. El front lo usa cuando el documento evaluado no corresponde a una persona existente y debe enviar los datos personales y ubicacion.
        /// La persona NO se crea en la base de datos todavía; los datos se guardan temporalmente en Redis.
        /// La persona se crea definitivamente cuando el usuario establece su contraseña (CompleteInitialPassword).
        /// Requiere el header <c>X-Flow-Id</c> obtenido de EvaluateDocument.
        /// </remarks>
        /// <param name="request">Datos personales de la nueva persona.</param>
        /// <param name="flowId">Identificador de la sesion de registro, enviado en el header <c>X-Flow-Id</c>.</param>
        /// <returns>Resultado de la confirmacion. La persona queda pendiente hasta que se establezca la contraseña.</returns>
        /// <response code="200">Persona nueva confirmada correctamente. Se envió mail de activación.</response>
        /// <response code="400">Datos invalidos, captcha invalido, sesion expirada o regla funcional no cumplida.</response>
        [AllowAnonymous]
        [RequireCaptcha(CaptchaActions.ConfirmNewPerson, CaptchaValidationMode.ScoreOnly)]
        [HttpPost("confirm-new-person")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmNewPerson(
            [FromBody] RegisterPersonRequest request,
            [FromHeader(Name = FlowIdHeaderName)] string? flowId = null)
        {
            var flowError = await ValidarFlowEvaluadoAsync(flowId);
            if (flowError != null) return flowError;

            var result = await registroFlowService.ConfirmNewPersonAsync(request, flowId!);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Confirma una solicitud de alta previamente generada.
        /// </summary>
        /// <remarks>
        /// Endpoint publico protegido por captcha. El front lo usa cuando el flujo detecta una solicitud de alta pendiente y necesita completar la confirmacion sin volver a crear la persona.
        /// Requiere el header <c>X-Flow-Id</c> obtenido de EvaluateDocument.
        /// </remarks>
        /// <param name="request">Datos de la solicitud de alta que se confirma.</param>
        /// <param name="flowId">Identificador de la sesion de registro, enviado en el header <c>X-Flow-Id</c>.</param>
        /// <returns>Resultado de la confirmacion de la solicitud.</returns>
        /// <response code="200">Solicitud de alta confirmada correctamente.</response>
        /// <response code="400">Datos invalidos, captcha invalido, sesion expirada o regla funcional no cumplida.</response>
        [AllowAnonymous]
        [RequireCaptcha(CaptchaActions.ConfirmRegistrationRequest, CaptchaValidationMode.ScoreOnly)]
        [HttpPost("confirm-registration-request")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmRegistrationRequest(
            [FromBody] RegisterPersonRequest request,
            [FromHeader(Name = FlowIdHeaderName)] string? flowId = null)
        {
            var flowError = await ValidarFlowEvaluadoAsync(flowId);
            if (flowError != null) return flowError;

            if (request == null)
            {
                return ValidateResponse(OperationResult<object?>.IsFailed(
                    "REG_REQUEST_01",
                    nameof(ConfirmRegistrationRequest),
                    "La solicitud es obligatoria.",
                    400));
            }

            var documentError = await ValidarDocumentoDeFlowAsync(
                flowId!, request.DocumentType, request.DocumentNumber, nameof(ConfirmRegistrationRequest));
            if (documentError != null) return documentError;

            var result = await confirmRegistrationRequest.ExecuteAsync(request);

            if (result.Success && flowId != null)
            {
                await registroFlowService.UpdateStepAsync(flowId, RegistrationFlowConstants.Step.Confirmado);
            }

            return ValidateResponse(result);
        }
    }
}
