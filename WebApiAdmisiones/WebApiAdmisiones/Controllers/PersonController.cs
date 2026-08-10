using AppLogic.Identity.Dtos;
using AppLogic.Authentication.Dtos;
using AppLogic.Scholarships.Dtos;
using AppLogic.People.Dtos;
using AppLogic.People.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Utilities;
using WebApiAdmisiones.Models;
using WebApiAdmisiones.Security.Authentication;

namespace WebApiAdmisiones.Controllers
{
    [Authorize]
    [ApiController]
    [Route("person")]
    public class PersonController(
        IGetPersonDetails getPersonDetails,
        IUpdatePersonDetails updatePersonDetails,
        IValidatePhoneNumber validatePhoneNumber,
        IGetMyEnrollments getMyEnrollments,
        IChangePassword changePassword,
        IGetPersonPhoto getPersonPhoto,
        IUploadPersonPhoto uploadPersonPhoto,
        IGetPersonIdentityDocument getPersonIdentityDocument,
        IUploadPersonIdentityDocument uploadPersonIdentityDocument,
        ILogger<PersonController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<PersonController>(logger, currentUser)
    {
        #region PERSONA

        /// <summary>
        /// Obtiene los datos de la persona autenticada.
        /// </summary>
        /// <returns>Datos de la persona.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="404">No se encontró la persona autenticada.</response>
        [HttpGet("details")]
        [ProducesResponseType(typeof(OperationResult<PersonDetailsResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<PersonDetailsResponse>), 404)]
        public IActionResult GetPersonDetails()
        {
            var result = getPersonDetails.Execute(_currentUser.GetUserId());
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
        [HttpPut("details")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult UpdatePersonDetails([FromBody] UpdatePersonDetailsRequest request)
        {
            var result = updatePersonDetails.Execute(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Valida un telefono informado por el front para los datos de la persona.
        /// Publico: el flujo de registro lo necesita antes de que exista un token.
        /// </summary>
        /// <param name="phoneNumber">Telefono normalizado o ingresado por el usuario.</param>
        /// <param name="isPrimaryPhone">Indica si se valida como telefono principal.</param>
        /// <returns><c>true</c> si el telefono es valido para guardar.</returns>
        /// <response code="200">Telefono validado correctamente.</response>
        /// <response code="400">Telefono invalido o solicitud incompleta.</response>
        /// <response code="429">Se supero el limite de validaciones por minuto.</response>
        [AllowAnonymous]
        [EnableRateLimiting("PhoneValidation")]
        [HttpPost("validate-phone-number")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 429)]
        public IActionResult ValidatePhoneNumber(PhoneNumber phoneNumber, [FromQuery] bool isPrimaryPhone)
        {
            var result = validatePhoneNumber.Execute(phoneNumber, isPrimaryPhone);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene las inscripciones fresco 1 y 2 habilitadas de la persona autenticada.
        /// </summary>
        /// <returns>Lista de inscripciones con todos los campos expuestos por la vista.</returns>
        /// <response code="200">Datos obtenidos correctamente.</response>
        /// <response code="400">Solicitud inválida.</response>
        [HttpGet("enrollments")]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<MyEnrollmentsResponse>>), 200)]
        [ProducesResponseType(typeof(OperationResult<IEnumerable<MyEnrollmentsResponse>>), 400)]
        public IActionResult GetMyEnrollments()
        {
            var result = getMyEnrollments.Execute(_currentUser.GetUserId());
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
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        [ProducesResponseType(typeof(OperationResult<object>), 401)]
        [ProducesResponseType(typeof(OperationResult<object>), 500)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var result = await changePassword.ExecuteAsync(_currentUser.GetUserId(), request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Obtiene la foto de perfil de la persona autenticada.
        /// </summary>
        /// <returns>Imagen JPEG de la foto.</returns>
        /// <response code="200">Foto obtenida correctamente.</response>
        /// <response code="404">Foto no encontrada o sin imagen.</response>
        [HttpGet("photo")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(OperationResult<byte[]>), 404)]
        public IActionResult GetPersonPhoto()
        {
            var result = getPersonPhoto.Execute(_currentUser.GetUserId());
            if (!result.Success)
            {
                return ValidateResponse(result);
            }

            if (result.Data is null)
            {
                return ValidateResponse(OperationResult<byte[]>.IsFailed(
                    "GEN_FA_03",
                    nameof(GetPersonPhoto),
                    "Foto no encontrada.",
                    404));
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
        [HttpGet("identity-document")]
        [ProducesResponseType(typeof(OperationResult<PersonIdentityDocumentResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<PersonIdentityDocumentResponse>), 404)]
        [ProducesResponseType(typeof(OperationResult<PersonIdentityDocumentResponse>), 409)]
        public IActionResult GetPersonIdentityDocument()
        {
            var result = getPersonIdentityDocument.Execute(_currentUser.GetUserId());
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
        [HttpPost("photo")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        public IActionResult UploadPersonPhoto([FromBody] UploadPersonPhotoRequest? request)
        {
            if (request?.File is null)
            {
                return ValidateResponse(OperationResult<bool>.IsFailed(
                    "SUB_FOT_01",
                    nameof(UploadPersonPhoto),
                    "No se recibió el archivo adjunto.",
                    400,
                    default!));
            }

            var fileContent = request.File.Content ?? Array.Empty<byte>();
            var fileName = string.IsNullOrWhiteSpace(request.File.FileName)
                ? "image.jpg"
                : request.File.FileName;

            var result = uploadPersonPhoto.Execute(_currentUser.GetUserId(), fileContent, fileName);
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
        [HttpPost("identity-document")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        [ProducesResponseType(typeof(OperationResult<bool>), 409)]
        public IActionResult UploadPersonIdentityDocument([FromBody] UploadPersonIdentityDocumentRequest? request)
        {
            if (request?.Front is null || request.Back is null)
            {
                return ValidateResponse(OperationResult<bool>.IsFailed(
                    "SUB_DOC_01",
                    nameof(UploadPersonIdentityDocument),
                    "No se recibió el frente o el dorso del documento.",
                    400,
                    default!));
            }

            var result = uploadPersonIdentityDocument.Execute(
                _currentUser.GetUserId(),
                request.ExpirationDate,
                new IdentityDocumentFile
                {
                    FileName = request.Front.FileName,
                    Content = request.Front.Content
                },
                new IdentityDocumentFile
                {
                    FileName = request.Back.FileName,
                    Content = request.Back.Content
                });
            return ValidateResponse(result);
        }

        #endregion
    }
}
