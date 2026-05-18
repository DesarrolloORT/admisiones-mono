using System.Collections.Generic;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        IReconocimientoDocumento reconocimientoDocumentoService,
        ILogger<RegistroController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<RegistroController>(logger, currentUser)
    {
        [AllowAnonymous]
        [HttpPost("EvaluarDocumento")]
        [ProducesResponseType(typeof(OperationResult<RegistroEvaluacionResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<RegistroEvaluacionResponse>), 400)]
        public async Task<IActionResult> EvaluarDocumento([FromBody] RegistroEvaluarDocumentoRequest request)
        {
            var result = await registroService.EvaluarDocumentoAsync(request);
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpPost("VerificarIdentidad")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> VerificarIdentidad([FromBody] RegistroVerificarIdentidadRequest request)
        {
            var result = await registroService.VerificarIdentidadAsync(request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Analiza una imagen o PDF de documento usando Azure Document Intelligence, detecta si se trata
        /// de cédula uruguaya, pasaporte o documento extranjero admitido, y devuelve los datos extraídos.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("AnalizarAdjunto")]
        [ProducesResponseType(typeof(OperationResult<ReconocimientoDocumentoResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<ReconocimientoDocumentoResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<ReconocimientoDocumentoResponse>), 422)]
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

        [AllowAnonymous]
        [RequireCaptcha]
        [HttpPost("ConfirmarPersonaExistente")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmarPersonaExistente([FromBody] RegistroConfirmarPersonaExistenteRequest request)
        {
            var result = await registroService.ConfirmarPersonaExistenteAsync(request);
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [RequireCaptcha]
        [HttpPost("ConfirmarNuevaPersona")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmarNuevaPersona([FromBody] RegistroConfirmarNuevaPersonaRequest request)
        {
            var result = await registroService.ConfirmarNuevaPersonaAsync(request);
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [RequireCaptcha]
        [HttpPost("ConfirmarSolicitudAlta")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmarSolicitudAlta([FromBody] RegistroConfirmarSolicitudAltaRequest request)
        {
            var result = await registroService.ConfirmarSolicitudAltaAsync(request);
            return ValidateResponse(result);
        }
    }
}
