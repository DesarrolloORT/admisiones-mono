using System.Collections.Generic;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApiAdmisiones.Security;
using AzureService.DTOs;
using AzureService.Interfaces;
using WebApiAdmisiones.Models;
using Utilities;

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
        ILogger<RegistroController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<RegistroController>(logger, currentUser)
    {
        private const string FlowIdHeaderName = "X-Flow-Id";

        private string? ObtenerFlowId() =>
            HttpContext.Request.Headers[FlowIdHeaderName].FirstOrDefault();

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
        [HttpPost("EvaluarDocumento")]
        [ProducesResponseType(typeof(OperationResult<RegistroEvaluacionResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<RegistroEvaluacionResponse>), 400)]
        public async Task<IActionResult> EvaluarDocumento([FromBody] RegistroEvaluarDocumentoRequest request)
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
        /// Endpoint publico para confirmar que quien continua el registro conoce los datos requeridos de la persona encontrada por documento. El front debe usarlo antes de confirmar una persona existente.
        /// Requiere el header <c>X-Flow-Id</c> obtenido de EvaluarDocumento.
        /// </remarks>
        /// <param name="request">Datos de validacion de identidad asociados a la persona.</param>
        /// <returns>Resultado de la verificacion de identidad.</returns>
        /// <response code="200">Identidad verificada correctamente.</response>
        /// <response code="400">Datos invalidos, verificacion rechazada o sesion de registro expirada.</response>
        [AllowAnonymous]
        [HttpPost("VerificarIdentidad")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> VerificarIdentidad([FromBody] RegistroVerificarIdentidadRequest request)
        {
            var flowId = ObtenerFlowId();
            var flowValidation = await registroFlowService.ValidarFlowSessionAsync(flowId, stepEsperado: "evaluado");
            if (flowValidation != null) return ValidateResponse(flowValidation);

            if (request == null)
            {
                return ValidateResponse(OperationResult<object?>.IsFailed(
                    "REG_REQUEST_01",
                    nameof(VerificarIdentidad),
                    "La solicitud es obligatoria.",
                    400));
            }

            var documentoValidation = await registroFlowService.ValidarDocumentoFlowAsync(
                flowId!,
                request.TipoDocumento,
                request.Documento,
                nameof(VerificarIdentidad));
            if (documentoValidation != null) return ValidateResponse(documentoValidation);

            var result = await registroService.VerificarIdentidadAsync(request);

            if (result.Success && flowId != null)
            {
                await registroFlowService.ActualizarStepAsync(flowId, "identidad_verificada");
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
        [AllowAnonymous]
        [EnableRateLimiting("ReconocimientoDocumento")]
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

            return ValidateResponse(result);
        }

        /// <summary>
        /// Confirma el registro de una persona ya existente.
        /// </summary>
        /// <remarks>
        /// Endpoint publico protegido por captcha. El front lo usa despues de evaluar documento y verificar identidad para crear o actualizar el interes/solicitud de registro de una persona existente.
        /// Requiere el header <c>X-Flow-Id</c> obtenido de EvaluarDocumento.
        /// </remarks>
        /// <param name="request">Datos necesarios para confirmar la persona existente en el flujo de registro.</param>
        /// <returns>Resultado de la confirmacion del registro.</returns>
        /// <response code="200">Persona existente confirmada correctamente.</response>
        /// <response code="400">Datos invalidos, captcha invalido, sesion expirada o regla funcional no cumplida.</response>
        [AllowAnonymous]
        [RequireCaptcha]
        [HttpPost("ConfirmarPersonaExistente")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmarPersonaExistente([FromBody] RegistroConfirmarPersonaExistenteRequest request)
        {
            var flowId = ObtenerFlowId();
            var flowValidation = await registroFlowService.ValidarFlowSessionAsync(flowId, stepEsperado: "identidad_verificada");
            if (flowValidation != null) return ValidateResponse(flowValidation);

            var result = await registroFlowService.ConfirmarPersonaExistenteAsync(request, flowId!);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Confirma el registro de una persona nueva.
        /// </summary>
        /// <remarks>
        /// Endpoint publico protegido por captcha. El front lo usa cuando el documento evaluado no corresponde a una persona existente y debe enviar los datos personales, ubicacion y seleccion academica.
        /// La persona NO se crea en la base de datos todavía; los datos se guardan temporalmente en Redis.
        /// La persona se crea definitivamente cuando el usuario establece su contraseña (CompletarPassword).
        /// Requiere el header <c>X-Flow-Id</c> obtenido de EvaluarDocumento.
        /// </remarks>
        /// <param name="request">Datos personales y academicos de la nueva persona.</param>
        /// <returns>Resultado de la confirmacion. La persona queda pendiente hasta que se establezca la contraseña.</returns>
        /// <response code="200">Persona nueva confirmada correctamente. Se envió mail de activación.</response>
        /// <response code="400">Datos invalidos, captcha invalido, sesion expirada o regla funcional no cumplida.</response>
        [AllowAnonymous]
        [RequireCaptcha]
        [HttpPost("ConfirmarNuevaPersona")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmarNuevaPersona([FromBody] RegistroPersonaRequest request)
        {
            var flowId = ObtenerFlowId();
            var flowValidation = await registroFlowService.ValidarFlowSessionAsync(flowId, stepEsperado: "evaluado");
            if (flowValidation != null) return ValidateResponse(flowValidation);

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
        [RequireCaptcha]
        [HttpPost("ConfirmarSolicitudAlta")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmarSolicitudAlta([FromBody] RegistroPersonaRequest request)
        {
            var flowId = ObtenerFlowId();
            var flowValidation = await registroFlowService.ValidarFlowSessionAsync(flowId, stepEsperado: "evaluado");
            if (flowValidation != null) return ValidateResponse(flowValidation);

            if (request == null)
            {
                return ValidateResponse(OperationResult<object?>.IsFailed(
                    "REG_REQUEST_01",
                    nameof(ConfirmarSolicitudAlta),
                    "La solicitud es obligatoria.",
                    400));
            }

            var documentoValidation = await registroFlowService.ValidarDocumentoFlowAsync(
                flowId!,
                request.TipoDocumento,
                request.Documento,
                nameof(ConfirmarSolicitudAlta));
            if (documentoValidation != null) return ValidateResponse(documentoValidation);

            var result = await registroService.ConfirmarSolicitudAltaAsync(request);

            if (result.Success && flowId != null)
            {
                await registroFlowService.ActualizarStepAsync(flowId, "confirmado");
            }

            return ValidateResponse(result);
        }
    }
}
