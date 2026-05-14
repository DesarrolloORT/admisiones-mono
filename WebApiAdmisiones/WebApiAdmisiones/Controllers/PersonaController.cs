using AppLogic.DevartDTOs;
using AppLogic.IServices;
using AppLogic.Requests;
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

        ///// <summary>
        ///// Obtiene los datos de la persona autenticada.
        ///// </summary>
        ///// <returns>Datos de la persona.</returns>
        ///// <response code="200">Datos obtenidos correctamente.</response>
        ///// <response code="404">No se encontró la persona autenticada.</response>
        ///// <response code="400">Solicitud inválida.</response>
        //[HttpGet("Persona")]
        //[ProducesResponseType(typeof(OperationResult<DtoPersonaDevart>), 200)]
        //[ProducesResponseType(typeof(OperationResult<DtoPersonaDevart>), 400)]
        //[ProducesResponseType(typeof(OperationResult<DtoPersonaDevart>), 404)]
        //public IActionResult ObtenerPersona()
        //{
        //    var result = personaAdmisionService.ObtenerPersona(_currentUser.GetUserId());
        //    return ValidateResponse(result);
        //}

        ///// <summary>
        ///// Actualiza los datos personales editables de la persona autenticada.
        ///// </summary>
        ///// <param name="request">Datos personales a actualizar.</param>
        ///// <returns><c>true</c> si la actualización se realizó correctamente.</returns>
        ///// <response code="200">Datos actualizados correctamente.</response>
        ///// <response code="400">Los datos enviados son inválidos.</response>
        ///// <response code="404">No se encontró la persona autenticada.</response>
        //[HttpPut("Persona")]
        //[ProducesResponseType(typeof(OperationResult<bool>), 200)]
        //[ProducesResponseType(typeof(OperationResult<bool>), 400)]
        //[ProducesResponseType(typeof(OperationResult<bool>), 404)]
        //public IActionResult ActualizarPersona([FromBody] ActualizarPersonaRequest request)
        //{
        //    var result = personaAdmisionService.ActualizarPersona(_currentUser.GetUserId(), request);
        //    return ValidateResponse(result);
        //}

        ///// <summary>
        ///// Obtiene los datos de preinscripción (encuesta inicial) de la persona autenticada.
        ///// </summary>
        ///// <returns>Datos de preinscripción.</returns>
        ///// <response code="200">Datos obtenidos correctamente.</response>
        ///// <response code="404">No se encontraron datos de preinscripción para la persona.</response>
        ///// <response code="400">Solicitud inválida.</response>
        //[HttpGet("EncuestaInicialAdmision")]
        //[ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 200)]
        //[ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 400)]
        //[ProducesResponseType(typeof(OperationResult<DtoEncuestaIniAdmisionDevart>), 404)]
        //public IActionResult ObtenerEncuestaInicialAdmision()
        //{
        //    var result = personaAdmisionService.ObtenerEncuestaInicialAdmision(_currentUser.GetUserId());
        //    return ValidateResponse(result);
        //}

        ///// <summary>
        ///// Guarda los datos de la persona y registra la encuesta inicial de admisión.
        ///// </summary>
        ///// <param name="request">Datos de persona y encuesta a registrar.</param>
        ///// <returns><c>true</c> si la encuesta se guardó correctamente.</returns>
        ///// <response code="200">Encuesta guardada correctamente.</response>
        ///// <response code="400">Los datos enviados son inválidos.</response>
        ///// <response code="404">No se encontró la persona, el producto o el proceso indicado.</response>
        ///// <response code="409">Ya existe una encuesta para la misma persona, producto y comienzo.</response>
        //[HttpPost("DatosPersonaEncuesta")]
        //[ProducesResponseType(typeof(OperationResult<bool>), 200)]
        //[ProducesResponseType(typeof(OperationResult<bool>), 400)]
        //[ProducesResponseType(typeof(OperationResult<bool>), 404)]
        //[ProducesResponseType(typeof(OperationResult<bool>), 409)]
        //public IActionResult GuardarDatosPersonaEncuesta([FromBody] GuardarDatosPersonaEncuestaRequest request)
        //{
        //    var result = personaAdmisionService.GuardarDatosPersonaEncuesta(_currentUser.GetUserId(), request);
        //    return ValidateResponse(result);
        //}

        ///// <summary>
        ///// Obtiene la foto de perfil del alumno autenticado.
        ///// </summary>
        ///// <returns>Imagen JPEG de la foto.</returns>
        ///// <response code="200">Foto obtenida correctamente.</response>
        ///// <response code="404">Foto no encontrada o sin imagen.</response>
        //[HttpGet("FotoAlumno")]
        //[ProducesResponseType(typeof(FileContentResult), 200)]
        //[ProducesResponseType(typeof(OperationResult<byte[]>), 404)]
        //public IActionResult ObtenerFotoAlumno()
        //{
        //    var result = personaAdmisionService.ObtenerFotoAlumno(_currentUser.GetUserId());
        //    if (!result.Success)
        //    {
        //        return ValidateResponse(result);
        //    }

        //    if (result.Data is null)
        //    {
        //        return NotFound();
        //    }

        //    return File(result.Data, "image/jpeg");
        //}

        ///// <summary>
        ///// Obtiene el documento de identidad del alumno autenticado.
        ///// </summary>
        ///// <param name="tipo">Cara del documento: 1 = frente, 2 = dorso.</param>
        ///// <returns>Imagen JPEG del documento.</returns>
        ///// <response code="200">Imagen obtenida correctamente.</response>
        ///// <response code="400">Tipo de documento inválido.</response>
        ///// <response code="404">Documento no encontrado o sin imagen.</response>
        //[HttpGet("DocumentoAlumno")]
        //[ProducesResponseType(typeof(FileContentResult), 200)]
        //[ProducesResponseType(typeof(OperationResult<byte[]>), 400)]
        //[ProducesResponseType(typeof(OperationResult<byte[]>), 404)]
        //public IActionResult ObtenerDocumentoAlumno([FromQuery] int tipo)
        //{
        //    var result = personaAdmisionService.ObtenerDocumentoAlumno(_currentUser.GetUserId(), tipo);
        //    if (!result.Success)
        //    {
        //        return ValidateResponse(result);
        //    }

        //    if (result.Data is null)
        //    {
        //        return NotFound();
        //    }

        //    return File(result.Data, "image/jpeg");
        //}

        ///// <summary>
        ///// Sube la foto del alumno autenticado.
        ///// </summary>
        ///// <param name="request">JSON con el nombre del archivo y los bytes de la imagen.</param>
        ///// <returns><c>true</c> si la foto se guardó correctamente.</returns>
        ///// <response code="200">Archivo guardado correctamente.</response>
        ///// <response code="400">Request inválido o archivo no permitido.</response>
        ///// <response code="404">No se encontró la persona autenticada.</response>
        //[HttpPost("SubirFotoAlumno")]
        //[ProducesResponseType(typeof(OperationResult<bool>), 200)]
        //[ProducesResponseType(typeof(OperationResult<bool>), 400)]
        //[ProducesResponseType(typeof(OperationResult<bool>), 404)]
        //public IActionResult SubirFotoAlumno([FromBody] SubirFotoAlumnoRequest request)
        //{
        //    var fileContent = request.ArchivoAdjunto.Archivo ?? Array.Empty<byte>();
        //    var fileName = string.IsNullOrWhiteSpace(request.ArchivoAdjunto.NombreArchivo)
        //        ? "image.jpg"
        //        : request.ArchivoAdjunto.NombreArchivo;

        //    var result = personaAdmisionService.SubirFotoAlumno(_currentUser.GetUserId(), fileContent, fileName);
        //    return ValidateResponse(result);
        //}

        ///// <summary>
        ///// Sube un documento del alumno autenticado para el tipo y fecha de vencimiento indicados.
        ///// </summary>
        ///// <param name="request">JSON con tipo, fecha de vencimiento, nombre del archivo y bytes del documento.</param>
        ///// <returns><c>true</c> si el documento se guardó correctamente.</returns>
        ///// <response code="200">Archivo guardado correctamente.</response>
        ///// <response code="400">Request inválido o archivo no permitido.</response>
        ///// <response code="404">No se encontró la persona autenticada.</response>
        //[HttpPost("SubirDocumentoAlumno")]
        //[ProducesResponseType(typeof(OperationResult<bool>), 200)]
        //[ProducesResponseType(typeof(OperationResult<bool>), 400)]
        //[ProducesResponseType(typeof(OperationResult<bool>), 404)]
        //public IActionResult SubirDocumentoAlumno([FromBody] UploadDocumentoAlumnoRequest request)
        //{
        //    var fileContent = request.ArchivoAdjunto.Archivo ?? Array.Empty<byte>();
        //    var fileName = request.ArchivoAdjunto.NombreArchivo ?? string.Empty;
        //    var result = personaAdmisionService.SubirDocumentoAlumno(_currentUser.GetUserId(), request.Tipo, request.Fecha, fileContent, fileName);
        //    return ValidateResponse(result);
        //}

        #endregion
    }
}
