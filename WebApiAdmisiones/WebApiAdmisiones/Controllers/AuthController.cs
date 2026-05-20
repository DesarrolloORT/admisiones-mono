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
        IPasswordActivationService passwordActivationService,
        IConfiguration configuration,
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
        /// Endpoint publico para iniciar sesion. El front debe enviar codigo de persona y password; si la autenticacion es correcta, la API setea las cookies de access token y refresh token automaticamente.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("Login")]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 404)]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            var result = await loginService.AutenticarUsuarioLDAPAsync(request.CodigoPersona, request.Password);

            if (result.Success && result.Data != null)
            {

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Usuario {CodigoPersona} autenticado exitosamente", request.CodigoPersona);
                }

                SetAuthenticationCookies(result.Data);

                // No retornar los tokens en el body.
                // Los tokens ya fueron establecidos como cookies HttpOnly.
            }

            return ValidateResponse(result);
        }

        /// <summary>
        /// Valida el link de creacion de password inicial y crea una sesion temporal.
        /// </summary>
        /// <param name="request">Token de activacion recibido por mail.</param>
        /// <returns>Resultado de validacion del link. El token temporal no se devuelve en el body; se emite como cookie HttpOnly.</returns>
        /// <response code="200">Link valido. La cookie temporal X-Password-Activation fue establecida.</response>
        /// <response code="400">El token no fue enviado o el request es invalido.</response>
        /// <response code="401">El token expiro, fue manipulado, ya fue usado o no coincide con el hash guardado en la persona.</response>
        /// <response code="500">Error interno al validar el link.</response>
        /// <remarks>
        /// Endpoint publico usado por el frontend cuando el usuario abre el link recibido por mail.
        /// 
        /// Este endpoint no autentica al usuario para consumir servicios normales. Solo emite una cookie temporal
        /// llamada X-Password-Activation, con duracion configurada por PasswordActivation:SessionMinutes.
        /// 
        /// Flujo esperado:
        /// 1. El frontend recibe el token desde la URL del mail.
        /// 2. Llama a este endpoint enviando el token en el body.
        /// 3. Si el token es valido, la API setea X-Password-Activation.
        /// 4. El frontend muestra el formulario para crear la password inicial.
        /// 
        /// Ejemplo de request:
        /// 
        ///     POST /Auth/ActivarLinkPassword
        ///     {
        ///       "token": "eyJhbGciOiJIUzI1NiIs..."
        ///     }
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("ActivarLinkPassword")]
        [ProducesResponseType(typeof(OperationResult<DtoPasswordActivationSession>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoPasswordActivationSession>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoPasswordActivationSession>), 401)]
        [ProducesResponseType(typeof(OperationResult<DtoPasswordActivationSession>), 500)]
        public async Task<IActionResult> ActivarLinkPassword([FromBody] DtoActivarLinkPasswordRequest request)
        {
            var result = await passwordActivationService.ActivarLinkPasswordAsync(request.Token);

            if (result.Success && result.Data?.SessionToken != null)
            {
                var sessionMinutes = configuration.GetValue<int?>("PasswordActivation:SessionMinutes") ?? 15;
                CookieAuthenticationHelper.SetPasswordActivationCookie(
                    HttpContext,
                    result.Data.SessionToken,
                    sessionMinutes);
            }

            return ValidateResponse(result);
        }

        /// <summary>
        /// Completa la creacion de password inicial usando la cookie temporal del link.
        /// </summary>
        /// <param name="request">Nueva password elegida por el usuario.</param>
        /// <returns>Resultado de autenticacion normal. Los tokens de sesion se emiten como cookies HttpOnly.</returns>
        /// <response code="200">Password creada correctamente. Se elimina X-Password-Activation y se establecen X-Access-Token y X-Refresh-Token.</response>
        /// <response code="400">La nueva password no cumple las reglas de validacion.</response>
        /// <response code="401">No existe cookie temporal, expiro o no corresponde al flujo de activacion.</response>
        /// <response code="404">No se encontro la persona asociada a la sesion temporal.</response>
        /// <response code="500">Error interno al completar la password inicial o al cambiarla en LDAP.</response>
        /// <remarks>
        /// Endpoint publico pero no anonimo funcionalmente: no usa Authorize porque no debe aceptar el JWT normal.
        /// Valida explicitamente la cookie temporal X-Password-Activation generada por Auth/ActivarLinkPassword.
        /// 
        /// Esta cookie solo sirve para este endpoint. No permite consumir otros servicios de la API.
        /// 
        /// Si LDAP falla, el hash del link se conserva para permitir reintentar mientras el link siga vigente.
        /// Si LDAP responde correctamente, la API limpia el hash guardado, elimina la cookie temporal y emite
        /// las cookies normales X-Access-Token y X-Refresh-Token.
        /// 
        /// Ejemplo de request:
        /// 
        ///     POST /Auth/CompletarPasswordInicial
        ///     {
        ///       "passwordNueva": "NuevaPassword1!"
        ///     }
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("CompletarPasswordInicial")]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 404)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 500)]
        public async Task<IActionResult> CompletarPasswordInicial([FromBody] DtoCompletarPasswordInicialRequest request)
        {
            var sessionToken = CookieAuthenticationHelper.GetPasswordActivationTokenFromCookie(HttpContext);
            var sessionResult = passwordActivationService.ValidarSessionToken(sessionToken ?? string.Empty);

            if (!sessionResult.Success)
            {
                CookieAuthenticationHelper.ClearPasswordActivationCookie(HttpContext);
                return ValidateResponse(OperationResult<DtoAuthenticationResponse>.IsFailed(
                    sessionResult.ErrorCode,
                    nameof(CompletarPasswordInicial),
                    sessionResult.Message,
                    sessionResult.HttpCode,
                    default!));
            }

            var result = await loginService.CompletarPasswordInicialAsync(sessionResult.Data, request);

            if (result.Success && result.Data != null)
            {
                CookieAuthenticationHelper.ClearPasswordActivationCookie(HttpContext);
                SetAuthenticationCookies(result.Data);
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
                SetAuthenticationCookies(result.Data);
            }

            return Ok(result);
        }

        /// <summary>
        /// Recuperar contraseña
        /// </summary>
        /// <param name="request">Datos de la persona a recuperar.</param>
        /// <returns>Mensaje generico del proceso de recupero.</returns>
        /// <response code="200">Solicitud recibida. Si los datos coinciden, se envia un mail con link de recupero.</response>
        /// <response code="400">El request o el formato del documento es invalido.</response>
        /// <remarks>
        /// Endpoint publico para iniciar el flujo de recuperacion de password. El front envia tipo de documento, documento y primer apellido.
        /// Si los datos coinciden, la API envia un mail con link seguro de recupero. La respuesta es generica para no revelar si la persona existe.
        /// </remarks>
        [HttpPost("RecuperarContraseña")]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
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

        private void SetAuthenticationCookies(DtoAuthenticationResponse data)
        {
            if (string.IsNullOrWhiteSpace(data.AccessToken) || string.IsNullOrWhiteSpace(data.RefreshToken))
            {
                return;
            }

            var accessMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES_ADMISIONES") ?? "15");
            var refreshDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRE_ADMISIONES") ?? "7");

            CookieAuthenticationHelper.SetAccessTokenCookie(HttpContext, data.AccessToken, accessMinutes);
            CookieAuthenticationHelper.SetRefreshTokenCookie(HttpContext, data.RefreshToken, refreshDays);
        }

        #endregion
    }
}
