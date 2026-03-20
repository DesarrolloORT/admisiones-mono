using AppLogic.DevartDTOs;
using AppLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Models;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PersonaController(
        IPersonaAdmisionService personaAdmisionService,
        ILogger<PersonaController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<PersonaController>(logger, currentUser)
    {
        #region PERSONA

        /// <summary>
        /// Obtiene los datos de la persona autenticada.
        /// </summary>
        /// <returns>Datos de la persona.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("Persona")]
        [ProducesResponseType(typeof(OperationResult<DtoPersonaDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoPersonaDevart>), 204)]
        [ProducesResponseType(typeof(OperationResult<DtoPersonaDevart>), 400)]
        public IActionResult ObtenerPersona()
        {
            var result = personaAdmisionService.ObtenerPersona(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene los datos de preinscripción (encuesta inicial) de la persona autenticada.
        /// </summary>
        /// <returns>Datos de preinscripción.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="204">Sin datos.</response>
        /// <response code="400">Error interno del servidor.</response>
        [HttpGet("EncuestaInicialAdmision")]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 204)]
        [ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 400)]
        public IActionResult ObtenerEncuestaInicialAdmision()
        {
            var result = personaAdmisionService.ObtenerEncuestaInicialAdmision(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene la foto de perfil del alumno autenticado.
        /// </summary>
        /// <returns>Imagen JPEG de la foto.</returns>
        /// <response code="200">Foto obtenida correctamente.</response>
        /// <response code="404">Foto no encontrada o sin imagen.</response>
        [HttpGet("FotoAlumno")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(OperationResult<byte[]>), 404)]
        public IActionResult ObtenerFotoAlumno()
        {
            var result = personaAdmisionService.ObtenerFotoAlumno(_currentUser.GetUserId());
            if (!result.Success)
                return ValidateResponse(result);

            if (result.Data is null)
                return NotFound();

            return File(result.Data, "image/jpeg");
        }

        /// <summary>
        /// Obtiene el documento de identidad (cédula) del alumno autenticado.
        /// </summary>
        /// <param name="tipo">Cara del documento: 1 = frente, 2 = dorso.</param>
        /// <returns>Imagen JPEG del documento.</returns>
        /// <response code="200">Imagen obtenida correctamente.</response>
        /// <response code="204">El documento está vencido.</response>
        /// <response code="400">Tipo de documento inválido.</response>
        /// <response code="404">Documento no encontrado o sin imagen.</response>
        [HttpGet("DocumentoAlumno")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(OperationResult<byte[]>), 204)]
        [ProducesResponseType(typeof(OperationResult<byte[]>), 400)]
        [ProducesResponseType(typeof(OperationResult<byte[]>), 404)]
        public IActionResult ObtenerDocumentoAlumno([FromQuery] int tipo)
        {
            var result = personaAdmisionService.ObtenerDocumentoAlumno(_currentUser.GetUserId(), tipo);
            if (!result.Success)
                return ValidateResponse(result);

            if (result.Data is null)
                return NotFound();

            return File(result.Data, "image/jpeg");
        }

        /// <summary>
        /// Sube la foto del alumno autenticado.
        /// </summary>
        /// <param name="request">JSON con el nombre del archivo y los bytes de la imagen.</param>
        /// <returns>true si la foto se guardó correctamente.</returns>
        /// <response code="200">Archivo guardado correctamente.</response>
        /// <response code="400">Request inválido o archivo no permitido.</response>
        /// <response code="404">No se encontró la persona autenticada.</response>
        [HttpPost("SubirFotoAlumno")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult SubirFotoAlumno([FromBody] SubirFotoAlumnoRequest request)
        {
            var fileContent = request.ArchivoAdjunto.Archivo ?? Array.Empty<byte>();
            var fileName = string.IsNullOrWhiteSpace(request.ArchivoAdjunto.NombreArchivo)
                ? "image.jpg"
                : request.ArchivoAdjunto.NombreArchivo;

            var result = personaAdmisionService.SubirFotoAlumno(_currentUser.GetUserId(), fileContent, fileName);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Sube un documento del alumno autenticado para el tipo y fecha de vencimiento indicados.
        /// </summary>
        /// <param name="request">JSON con tipo, fecha de vencimiento, nombre del archivo y bytes del documento.</param>
        /// <returns>true si el documento se guardó correctamente.</returns>
        /// <response code="200">Archivo guardado correctamente.</response>
        /// <response code="400">Request inválido o archivo no permitido.</response>
        /// <response code="404">No se encontró la persona autenticada.</response>
        [HttpPost("SubirDocumentoAlumno")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult SubirDocumentoAlumno([FromBody] UploadDocumentoAlumnoRequest request)
        {
            var fileContent = request.ArchivoAdjunto.Archivo ?? Array.Empty<byte>();
            var fileName = request.ArchivoAdjunto.NombreArchivo ?? string.Empty;
            var result = personaAdmisionService.SubirDocumentoAlumno(_currentUser.GetUserId(), request.Tipo, request.Fecha, fileContent, fileName);
            return ValidateResponse(result);
        }

        #endregion
    }
}
