using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Utilities;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController(
        IAuthService loginService,
        ILogger<AuthController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<AuthController>(logger, currentUser)
    {
        #region AUTH

        /// <summary>
        /// Autentica un usuario mediante LDAP.
        /// Los tokens se devuelven como cookies HttpOnly seguras, no en el body de la respuesta.
        /// </summary>
        /// <param name="request">Datos de autenticación del usuario.</param>
        /// <returns>Resultado de la autenticación con información del usuario. Los tokens se envían como cookies.</returns>
        /// <response code="200">Autenticación exitosa. Las cookies X-Access-Token y X-Refresh-Token han sido establecidas.</response>
        /// <response code="400">Error en los datos de entrada.</response>
        /// <response code="401">Credenciales inválidas.</response>
        /// <remarks>
        /// Endpoint publico para iniciar sesion. El front debe enviar tipoDocumento, documento y password; si la autenticacion es correcta, la API setea las cookies de access token y refresh token automaticamente.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("Login")]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 404)]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            var result = await loginService.AutenticarUsuarioLDAPAsync(request.TipoDocumento, request.Documento, request.Password);

            if (result.Success && result.Data != null)
            {

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Usuario {CodigoPersona} autenticado exitosamente", request.Documento);
                }

                var accessMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES_ADMISIONES") ?? "15");
                var refreshDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRE_ADMISIONES") ?? "7");

                // Establecer los tokens como cookies HttpOnly seguras.
                CookieAuthenticationHelper.SetAccessTokenCookie(HttpContext, result.Data.AccessToken!, accessMinutes);
                CookieAuthenticationHelper.SetRefreshTokenCookie(HttpContext, result.Data.RefreshToken!, refreshDays);

                // No retornar los tokens en el body.
                // Los tokens ya fueron establecidos como cookies HttpOnly.
            }

            return ValidateResponse(result);
        }

        /// <summary>
        /// Cierra la sesión del usuario eliminando las cookies de autenticación.
        /// </summary>
        /// <returns>Resultado de la operación.</returns>
        /// <response code="200">Logout exitoso.</response>
        /// <remarks>
        /// Endpoint autenticado para finalizar la sesion del usuario actual. El front puede llamarlo al cerrar sesion para limpiar las cookies HttpOnly emitidas por la API.
        /// </remarks>
        [HttpPost("Logout")]
        [ProducesResponseType(typeof(OperationResult<string>), 200)]
        [ProducesResponseType(typeof(OperationResult<string>), 200)]
        public IActionResult Logout()
        {
            // Eliminar las cookies de autenticación.
            CookieAuthenticationHelper.ClearAuthenticationCookies(HttpContext);

            var result = OperationResult<string>.Ok("Sesión cerrada correctamente.", nameof(Logout));
            return ValidateResponse(result);
        }

        /// <summary>
        /// Renueva el access token usando el refresh token almacenado en cookies.
        /// Este endpoint valida el refresh token contra la base de datos y genera nuevos tokens.
        /// </summary>
        /// <returns>Resultado de la operación con mensaje de éxito.</returns>
        /// <response code="200">Tokens renovados correctamente. Las cookies se actualizan automáticamente.</response>
        /// <response code="401">Refresh token inválido, expirado o no encontrado.</response>
        /// <response code="404">Usuario no encontrado en la base de datos.</response>
        /// <remarks>
        /// Endpoint autenticado por cookie de refresh token. El front no necesita enviar el token en el body; debe llamar este endpoint cuando expire el access token y la API actualizara las cookies si el refresh token sigue vigente.
        /// </remarks>
        [HttpPost("RefreshToken")]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 404)]
        public async Task<IActionResult> RefreshToken()
        {
            // 1. Leer refresh token de la cookie (HTTP concern).
            var refreshToken = CookieAuthenticationHelper.GetRefreshTokenFromCookie(HttpContext);

            if (string.IsNullOrEmpty(refreshToken))
            {
                var errorResult = OperationResult<DtoAuthenticationResponse>.IsFailed(
                    errorCode: "AUTH_RT_01",
                    originMethod: nameof(RefreshToken),
                    message: "Refresh token no encontrado en las cookies.",
                    httpCode: 401);
                CookieAuthenticationHelper.ClearAuthenticationCookies(HttpContext);
                return Unauthorized(errorResult);
            }


            // 2. Delegar toda la lógica de negocio al servicio.
            var result = await loginService.RefrescarTokensAsync(refreshToken);

            if (!result.Success)
            {
                // Limpiar cookies si falló la renovación
                CookieAuthenticationHelper.ClearAuthenticationCookies(HttpContext);
                return result.HttpCode == 404 ? NotFound(result) : Unauthorized(result);
            }

            if (!result.Success)
            {
                // Limpiar cookies si falló la renovación.
                CookieAuthenticationHelper.ClearAuthenticationCookies(HttpContext);
                return result.HttpCode == 404 ? NotFound(result) : Unauthorized(result);
            }

            // 3. Establecer cookies con los nuevos tokens (HTTP concern)
            if (result.Data != null)
            {
                var accessMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES_ADMISIONES") ?? "15");
                var refreshDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRE_ADMISIONES") ?? "7");

                CookieAuthenticationHelper.SetAccessTokenCookie(HttpContext, result.Data.AccessToken!, accessMinutes);
                CookieAuthenticationHelper.SetRefreshTokenCookie(HttpContext, result.Data.RefreshToken!, refreshDays);
            }

            return Ok(result);
        }

        /// <summary>
        /// Recuperar contraseña
        /// </summary>
        /// <param name="request">Datos de la persona a recuperar.</param>
        /// <returns>Resultado del proceso con mensaje y codigos funcionales del servicio.</returns>
        /// <response code="200">Recuperacion exitosa.</response>
        /// <response code="400">Error de validacion o de negocio.</response>
        /// <response code="500">Error interno no controlado.</response>
        /// <remarks>
        /// Endpoint publico para iniciar el flujo de recuperacion de password. El front envia los datos requeridos de la persona y la API ejecuta las validaciones funcionales antes de solicitar o disparar el recupero.
        /// </remarks>
        [HttpPost("RecuperarContraseña")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        [ProducesResponseType(typeof(OperationResult<object>), 500)]
        public async Task<IActionResult> RecuperarPassword([FromBody] DtoRecuperarPasswordRequest request)
        {
            var result = await loginService.RecuperarPassword(request);
            return ValidateResponse(result);
        }

        /// <summary>
        /// Cambia la contraseña del usuario autenticado.
        /// </summary>
        /// <param name="request">Password actual y nueva password.</param>
        /// <returns>Resultado del cambio de contraseña.</returns>
        /// <response code="200">Contraseña actualizada correctamente.</response>
        /// <response code="400">Error de validacion o de negocio.</response>
        /// <response code="401">Usuario no autenticado.</response>
        /// <response code="500">Error interno no controlado.</response>
        /// <remarks>
        /// Endpoint autenticado para actualizar la password del usuario actual. El codigo de persona se toma del token, por lo que el front solo debe enviar password actual y nueva password.
        /// </remarks>
        [Authorize]
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

            var result = await loginService.CambiarPasswordAsync(_currentUser.UserId.Value, request);
            return ValidateResponse(result);
        }

        #endregion
    }
}
