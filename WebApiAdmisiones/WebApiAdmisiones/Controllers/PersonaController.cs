using AppLogic.Dtos.Autenticacion;
using AppLogic.Dtos.Becas;
using AppLogic.Dtos.Personas;
using AppLogic.DevartDTOs;
using AppLogic.IServices.Personas;
using AppLogic.Services.Inscripciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Models;
using WebApiAdmisiones.Security.Authentication;

namespace WebApiAdmisiones.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PersonaController(
        IPersonaService personaService,
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
        /// <response code="404">No se encontró la persona autenticada.</response>
        [HttpGet("DatosPersona")]
        [ProducesResponseType(typeof(OperationResult<DtoDatosPersona>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoDatosPersona>), 404)]
        public IActionResult ObtenerDatosPersona()
        {
            var result = personaService.ObtenerDatosPersona(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Actualiza los datos editables de la persona autenticada.
        /// </summary>
        /// <param name="request">Datos editables de la persona.</param>
        /// <returns><c>true</c> si la actualización se realizó correctamente.</returns>
        /// <response code="200">Datos actualizados correctamente.</response>
        /// <response code="400">Los datos enviados son inválidos.</response>
        /// <response code="404">No se encontró la persona autenticada.</response>
        [HttpPut("DatosPersona")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult ActualizarDatosPersona([FromBody] DtoActualizarDatosPersonaRequest request)
        {
            var result = personaService.ActualizarDatosPersona(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Valida un telefono informado por el front para los datos de la persona.
        /// </summary>
        /// <param name="telefonoValidar">Telefono normalizado o ingresado por el usuario.</param>
        /// <param name="telefono1">Indica si se valida como telefono principal.</param>
        /// <returns><c>true</c> si el telefono es valido para guardar.</returns>
        /// <response code="200">Telefono validado correctamente.</response>
        /// <response code="400">Telefono invalido o solicitud incompleta.</response>
        [HttpPost("ValidarTelefono")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        public IActionResult ValidarTelefono(DtoTelefono telefonoValidar, [FromQuery] bool telefono1)
        {
            var result = personaService.EsTelefonoValidoFront(telefonoValidar, telefono1);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las inscripciones fresco 1 y 2 habilitadas de la persona autenticada.
        /// </summary>
        /// <returns>Lista de inscripciones con todos los campos expuestos por la vista.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("Inscripciones")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>>), 400)]
        public IActionResult ObtenerMisInscripciones()
        {
            var result = personaService.ObtenerMisInscripciones(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las becas de la persona autenticada.
        /// </summary>
        /// <returns>Lista mock de becas para la vista del front.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("Becas")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoBecaPersona>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<DtoBecaPersona>>), 400)]
        public IActionResult ObtenerMisBecas()
        {
            var becas = new List<DtoBecaPersona>
            {
                new()
                {
                    IdBeca = 1,
                    IdPostulacion = 1001,
                    Nombre = "Fondo de Excelencia Academica",
                    Carrera = "Licenciatura en Diseno Grafico",
                    Estado = "En proceso",
                    FechaCierrePostulacion = new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Local),
                    FechaPrueba = new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Local),
                    AccionPrincipal = "Continuar postulacion",
                    PuedeContinuarPostulacion = true,
                    PuedeDescargarMaterialEstudio = false
                },
                new()
                {
                    IdBeca = 2,
                    IdPostulacion = 1002,
                    Nombre = "Fondo de Excelencia Academica",
                    Carrera = "Licenciatura en Diseno Grafico",
                    FechaPrueba = new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Local),
                    FechaResultados = new DateTime(2025, 7, 24, 0, 0, 0, DateTimeKind.Local),
                    AccionPrincipal = "Descargar material de estudio",
                    PuedeContinuarPostulacion = false,
                    PuedeDescargarMaterialEstudio = true,
                    UrlMaterialEstudio = "#"
                }
            };

            var result = OperationResult<IEnumerable<DtoBecaPersona>>.Ok(becas, nameof(ObtenerMisBecas));
            return ValidateResponse(result);
        }

        /// <summary>
        /// Cambia la contraseña del usuario autenticado.
        /// </summary>
        /// <param name="request">Password actual y nueva password.</param>
        /// <returns>Resultado del cambio de contraseña.</returns>
        /// <response code="200">Contraseña actualizada correctamente.</response>
        /// <response code="400">Error de validación o de negocio.</response>
        /// <response code="401">Usuario no autenticado.</response>
        /// <response code="500">Error interno no controlado.</response>
        [HttpPost("CambiarContraseña")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        [ProducesResponseType(typeof(OperationResult<object>), 401)]
        [ProducesResponseType(typeof(OperationResult<object>), 500)]
        public async Task<IActionResult> CambiarPassword([FromBody] DtoCambiarPasswordRequest request)
        {
            if (!_currentUser.UserId.HasValue)
            {
                var errorResult = OperationResult<object>.IsFailed(
                    errorCode: "CAM_PAS_03",
                    originMethod: nameof(CambiarPassword),
                    message: "Usuario no autenticado.",
                    httpCode: 401);

                return ValidateResponse(errorResult);
            }

            var result = await personaService.CambiarPasswordAsync(_currentUser.UserId.Value, request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene la foto de perfil de la persona autenticada.
        /// </summary>
        /// <returns>Imagen JPEG de la foto.</returns>
        /// <response code="200">Foto obtenida correctamente.</response>
        /// <response code="404">Foto no encontrada o sin imagen.</response>
        [HttpGet("Foto")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(OperationResult<byte[]>), 404)]
        public IActionResult ObtenerFotoPersona()
        {
            var result = personaService.ObtenerFotoPersona(_currentUser.GetUserId());
            if (!result.Success)
            {
                return ValidateResponse(result);
            }

            if (result.Data is null)
            {
                return NotFound();
            }

            return File(result.Data, "image/jpeg");
        }

        /// <summary>
        /// Obtiene el documento de identidad de la persona autenticada.
        /// </summary>
        /// <returns>Frente y dorso del documento disponibles.</returns>
        /// <response code="200">Imagen obtenida correctamente.</response>
        /// <response code="404">Documento no encontrado o sin imagen.</response>
        /// <response code="409">El documento existe pero no esta en un estado valido para ser devuelto.</response>
        [HttpGet("Documento")]
        [ProducesResponseType(typeof(OperationResult<DtoDocumentoPersonaResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoDocumentoPersonaResponse>), 404)]
        [ProducesResponseType(typeof(OperationResult<DtoDocumentoPersonaResponse>), 409)]
        public IActionResult ObtenerDocumentoPersona()
        {
            var result = personaService.ObtenerDocumentoPersona(_currentUser.GetUserId());
            return ValidateResponse(result);
        }

        /// <summary>
        /// Sube la foto de la persona autenticada.
        /// </summary>
        /// <param name="request">JSON con el nombre del archivo y los bytes de la imagen.</param>
        /// <returns><c>true</c> si la foto se guardo correctamente.</returns>
        /// <response code="200">Archivo guardado correctamente.</response>
        /// <response code="400">Request invalido o archivo no permitido.</response>
        /// <response code="404">No se encontro la persona autenticada.</response>
        [HttpPost("SubirFoto")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult SubirFotoPersona([FromBody] SubirFotoPersonaRequest request)
        {
            var fileContent = request.ArchivoAdjunto.Archivo ?? Array.Empty<byte>();
            var fileName = string.IsNullOrWhiteSpace(request.ArchivoAdjunto.NombreArchivo)
                ? "image.jpg"
                : request.ArchivoAdjunto.NombreArchivo;

            var result = personaService.SubirFotoPersona(_currentUser.GetUserId(), fileContent, fileName);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Sube frente y dorso del documento de la persona autenticada para la fecha de vencimiento indicada.
        /// </summary>
        /// <param name="request">JSON con fecha de vencimiento, frente y dorso del documento.</param>
        /// <returns><c>true</c> si el documento se guardo correctamente.</returns>
        /// <response code="200">Archivo guardado correctamente.</response>
        /// <response code="400">Request invalido o archivo no permitido.</response>
        /// <response code="404">No se encontro la persona autenticada.</response>
        [HttpPost("SubirDocumento")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult SubirDocumentoPersona([FromBody] UploadDocumentoPersonaRequest request)
        {
            var result = personaService.SubirDocumentoPersona(
                _currentUser.GetUserId(),
                request.Fecha,
                new DtoDocumentoPersonaArchivo
                {
                    NombreArchivo = request.Frente.NombreArchivo,
                    Archivo = request.Frente.Archivo
                },
                new DtoDocumentoPersonaArchivo
                {
                    NombreArchivo = request.Dorso.NombreArchivo,
                    Archivo = request.Dorso.Archivo
                });
            return ValidateResponse(result);
        }

        #endregion
    }
}
