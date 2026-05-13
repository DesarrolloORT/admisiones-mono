using System.Collections.Generic;
using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Controllers
{
    /// <summary>
    /// Endpoints públicos del flujo de registro previo a la autenticación.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class RegistroController(
        IRegistroService registroService,
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

        [AllowAnonymous]
        //[RequireCaptcha]
        [HttpPost("ConfirmarPersonaExistente")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmarPersonaExistente([FromBody] RegistroConfirmarPersonaExistenteRequest request)
        {
            var result = await registroService.ConfirmarPersonaExistenteAsync(request);
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        //[RequireCaptcha]
        [HttpPost("ConfirmarNuevaPersona")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmarNuevaPersona([FromBody] RegistroConfirmarNuevaPersonaRequest request)
        {
            var result = await registroService.ConfirmarNuevaPersonaAsync(request);
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        //[RequireCaptcha]
        [HttpPost("ConfirmarSolicitudAlta")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> ConfirmarSolicitudAlta([FromBody] RegistroConfirmarSolicitudAltaRequest request)
        {
            var result = await registroService.ConfirmarSolicitudAltaAsync(request);
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("TiposDocumentos")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>), 400)]
        public IActionResult ObtenerTipoDocumentos()
        {
            var result = registroService.ObtenerTipoDocumentos();
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("Comienzos")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<RegistroComienzoResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<RegistroComienzoResponse>>), 400)]
        public IActionResult ObtenerComienzos([FromQuery] long idCarrera)
        {
            var result = registroService.ObtenerComienzos(idCarrera);
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("PaisesEstadosCiudades")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoPaisDevart>>), 400)]
        public IActionResult ObtenerPaisesEstadosCiudades()
        {
            var result = registroService.ObtenerPaisesEstadosCiudades();
            return ValidateResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("Carreras")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<RegistroCarreraResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<RegistroCarreraResponse>>), 400)]
        public IActionResult ObtenerCarreras()
        {
            var result = registroService.ObtenerCarreras();
            return ValidateResponse(result);
        }
    }
}
