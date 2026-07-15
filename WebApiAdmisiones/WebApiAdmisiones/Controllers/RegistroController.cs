using AppLogic.Registro.Dtos;
using AppLogic.Registro.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AzureService.DTOs;
using AzureService.Interfaces;
using WebApiAdmisiones.Models;
using Utilities;
using WebApiAdmisiones.Security.Authentication;
using WebApiAdmisiones.Security.Captcha;
using AppLogic.Registro.Interfaces;

namespace WebApiAdmisiones.Controllers
{
    /// <summary>
    /// Endpoints públicos del flujo de registro previo a la autenticación.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class RegistroController(
        IRegistroService registroService,
        IRegistroFlowService registroFlowService,
        IReconocimientoDocumento reconocimientoDocumentoService,
        IRegistroDocumentoImagenCacheService documentoImagenCacheService,
        ILogger<RegistroController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<RegistroController>(logger, currentUser)
    {
        private const string FlowIdHeaderName = "X-Flow-Id";

        private string? ObtenerFlowId() =>
            HttpContext.Request.Headers[FlowIdHeaderName].FirstOrDefault();

        /// <summary>
        /// Lee el flowId del header y valida que la sesión de registro esté en el step "evaluado".
        /// </summary>
        private async Task<(string? FlowId, IActionResult? Error)> ValidarFlowEvaluadoAsync()
        {
            var flowId = ObtenerFlowId();
            var flowValidation = await registroFlowService.ValidarFlowSessionAsync(flowId, stepEsperado: "evaluado");
            return (flowId, flowValidation != null ? ValidateResponse(flowValidation) : null);
        }

        /// <summary>
        /// Valida que el documento recibido coincida con el de la sesión de registro.
        /// </summary>
        private async Task<IActionResult?> ValidarDocumentoDeFlowAsync(
            string flowId, string? tipoDocumento, string? documento, string originMethod)
        {
            var documentoValidation = await registroFlowService.ValidarDocumentoFlowAsync(
                flowId, tipoDocumento, documento, originMethod);
            return documentoValidation != null ? ValidateResponse(documentoValidation) : null;
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
        [RequireCaptcha(CaptchaActions.EvaluarDocumento, CaptchaValidationMode.ScoreOnly)]
        [HttpPost("EvaluarDocumento")]
        [ProducesResponseType(typeof(OperationResult<DtoRegistroEvaluacionResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoRegistroEvaluacionResponse>), 400)]
        public async Task<IActionResult> EvaluarDocumento([FromBody] DtoRegistroEvaluarDocumentoRequest request)
        {
            var result = await registroService.EvaluarDocumentoAsync(request);

            if (result.Success && result.Data != null && !result.Data.UsuarioExistente)
            {
                // Crear sesión de registro y adjuntar flowId a la respuesta
                var flowId = await registroFlowService.CrearFlowSessionAsync(
                    request?.TipoDocumento ?? string.Empty,
                    request?.Documento ?? string.Empty,
                    codigoPersona: null);

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
        /// Requiere el header <c>X-Flow-Id</c> obtenido de EvaluarDocumento.
        /// </remarks>
        /// <param name="request">Datos de validacion de identidad asociados a la persona.</param>
        /// <returns>Resultado de la verificacion de identidad.</returns>
        /// <response code="200">Identidad verificada correctamente.</response>
        /// <response code="400">Datos invalidos, verificacion rechazada o sesion de registro expirada.</response>
        [AllowAnonymous]
        [RequireCaptcha(CaptchaActions.VerificarIdentidad, CaptchaValidationMode.ScoreOnly)]
        [HttpPost("VerificarIdentidad")]
        [ProducesResponseType(typeof(OperationResult<DtoRegistroConfirmacionResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoRegistroConfirmacionResponse>), 400)]
        public async Task<IActionResult> VerificarIdentidad([FromBody] DtoRegistroVerificarIdentidadRequest request)
        {
            var (flowId, flowError) = await ValidarFlowEvaluadoAsync();
            if (flowError != null) return flowError;

            if (request == null)
            {
                return ValidateResponse(OperationResult<DtoRegistroConfirmacionResponse?>.IsFailed(
                    "REG_REQUEST_01",
                    nameof(VerificarIdentidad),
                    "La solicitud es obligatoria.",
                    400));
            }

            var documentoError = await ValidarDocumentoDeFlowAsync(
                flowId!, request.TipoDocumento, request.Documento, nameof(VerificarIdentidad));
            if (documentoError != null) return documentoError;

            var result = await registroService.VerificarIdentidadAsync(request);

            if (result.Success && flowId != null)
            {
                await registroFlowService.ActualizarStepAsync(flowId, RegistroFlowConstants.Step.Confirmado);
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
        [RequireCaptcha(CaptchaActions.AnalizarAdjunto, CaptchaValidationMode.ScoreOnly)]
        [HttpPost("AnalizarAdjunto")]
        [ProducesResponseType(typeof(OperationResult<ReconocimientoDocumentoResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<ReconocimientoDocumentoResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<ReconocimientoDocumentoResponse>), 422)]
        [ProducesResponseType(typeof(OperationResult<ReconocimientoDocumentoResponse>), 429)]
        [ProducesResponseType(typeof(OperationResult<ReconocimientoDocumentoResponse>), 500)]
        [ProducesResponseType(typeof(OperationResult<ReconocimientoDocumentoResponse>), 502)]
        [ProducesResponseType(typeof(OperationResult<ReconocimientoDocumentoResponse>), 504)]
        public async Task<IActionResult> AnalizarAdjunto([FromBody] ReconocimientoDocumentoApiRequest? request)
        {
            if (request?.ArchivoAdjunto is null)
            {
                return ValidateResponse(OperationResult<ReconocimientoDocumentoResponse>.IsFailed(
                    "REC_DOC_01",
                    nameof(AnalizarAdjunto),
                    "No se recibió la solicitud de reconocimiento.",
                    400,
                    default!));
            }

            var fileContent = request.ArchivoAdjunto.Archivo ?? Array.Empty<byte>();
            var fileName = FileValidator.ResolveFileName(request.ArchivoAdjunto.NombreArchivo, request.TipoMime);

            var result = await reconocimientoDocumentoService.ReconocerDocumentoAsync(
                new ReconocimientoDocumentoRequest
                {
                    Archivo = fileContent,
                    NombreArchivo = fileName,
                    TipoMime = request.TipoMime
                });

            if (result.Success && result.Data?.Campos is not null)
            {
                await IntentarGuardarImagenesDocumentoAsync(
                    result.Data,
                    fileContent,
                    fileName,
                    request.TipoMime);
            }

            return ValidateResponse(result);
        }

        private async Task IntentarGuardarImagenesDocumentoAsync(
            ReconocimientoDocumentoResponse reconocimiento,
            byte[] documentoOriginal,
            string nombreDocumento,
            string? tipoMime)
        {
            var documentoFrente = new DtoRegistroDocumentoArchivoTemporal
            {
                Archivo = documentoOriginal,
                NombreArchivo = nombreDocumento,
                ContentType = string.IsNullOrWhiteSpace(tipoMime)
                    ? "application/octet-stream"
                    : tipoMime
            };

            var caraPersona = reconocimiento.CaraPersona is null
                ? null
                : new DtoRegistroDocumentoArchivoTemporal
                {
                    Archivo = reconocimiento.CaraPersona.Archivo,
                    NombreArchivo = reconocimiento.CaraPersona.NombreArchivo,
                    ContentType = reconocimiento.CaraPersona.ContentType
                };

            await documentoImagenCacheService.GuardarImagenesTemporalesSiCorrespondeAsync(
                reconocimiento.Campos.TipoDocumento,
                reconocimiento.Campos.NumeroDocumento,
                reconocimiento.Campos.FechaVencimiento,
                documentoFrente,
                caraPersona);
        }

        /// <summary>
        /// Confirma el registro de una persona nueva.
        /// </summary>
        /// <remarks>
        /// Endpoint publico protegido por captcha. El front lo usa cuando el documento evaluado no corresponde a una persona existente y debe enviar los datos personales y ubicacion.
        /// La persona NO se crea en la base de datos todavía; los datos se guardan temporalmente en Redis.
        /// La persona se crea definitivamente cuando el usuario establece su contraseña (CompletarPassword).
        /// Requiere el header <c>X-Flow-Id</c> obtenido de EvaluarDocumento.
        /// </remarks>
        /// <param name="request">Datos personales de la nueva persona.</param>
        /// <returns>Resultado de la confirmacion. La persona queda pendiente hasta que se establezca la contraseña.</returns>
        /// <response code="200">Persona nueva confirmada correctamente. Se envió mail de activación.</response>
        /// <response code="400">Datos invalidos, captcha invalido, sesion expirada o regla funcional no cumplida.</response>
        [AllowAnonymous]
        [RequireCaptcha(CaptchaActions.ConfirmarNuevaPersona, CaptchaValidationMode.ScoreOnly)]
        [HttpPost("ConfirmarNuevaPersona")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmarNuevaPersona([FromBody] DtoRegistroPersonaRequest request)
        {
            var (flowId, flowError) = await ValidarFlowEvaluadoAsync();
            if (flowError != null) return flowError;

            var result = await registroFlowService.ConfirmarNuevaPersonaAsync(request, flowId!);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Confirma una solicitud de alta previamente generada.
        /// </summary>
        /// <remarks>
        /// Endpoint publico protegido por captcha. El front lo usa cuando el flujo detecta una solicitud de alta pendiente y necesita completar la confirmacion sin volver a crear la persona.
        /// Requiere el header <c>X-Flow-Id</c> obtenido de EvaluarDocumento.
        /// </remarks>
        /// <param name="request">Datos de la solicitud de alta que se confirma.</param>
        /// <returns>Resultado de la confirmacion de la solicitud.</returns>
        /// <response code="200">Solicitud de alta confirmada correctamente.</response>
        /// <response code="400">Datos invalidos, captcha invalido, sesion expirada o regla funcional no cumplida.</response>
        [AllowAnonymous]
        [RequireCaptcha(CaptchaActions.ConfirmarSolicitudAlta, CaptchaValidationMode.ScoreOnly)]
        [HttpPost("ConfirmarSolicitudAlta")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmarSolicitudAlta([FromBody] DtoRegistroPersonaRequest request)
        {
            var (flowId, flowError) = await ValidarFlowEvaluadoAsync();
            if (flowError != null) return flowError;

            if (request == null)
            {
                return ValidateResponse(OperationResult<object?>.IsFailed(
                    "REG_REQUEST_01",
                    nameof(ConfirmarSolicitudAlta),
                    "La solicitud es obligatoria.",
                    400));
            }

            var documentoError = await ValidarDocumentoDeFlowAsync(
                flowId!, request.TipoDocumento, request.Documento, nameof(ConfirmarSolicitudAlta));
            if (documentoError != null) return documentoError;

            var result = await registroService.ConfirmarSolicitudAltaAsync(request);

            if (result.Success && flowId != null)
            {
                await registroFlowService.ActualizarStepAsync(flowId, "confirmado");
            }

            return ValidateResponse(result);
        }
    }
}
