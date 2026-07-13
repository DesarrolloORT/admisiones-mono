using AppLogic.Autenticacion.Requests;
using AppLogic.Autenticacion.Responses;
using AppLogic.Autenticacion.Dtos;
using AppLogic.Autenticacion.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Utilities;
using WebApiAdmisiones.Security.Captcha;
using WebApiAdmisiones.Security.Authentication;

namespace WebApiAdmisiones.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController(
        IAuthService loginService,
        IPasswordActivationService passwordActivationService,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        ICurrentUserService currentUser,
        IDosFactoresAuthService dosFactoresService,
        ILoginFlowService loginFlowService)
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
        /// <response code="429">Demasiados intentos de login. Protección contra fuerza bruta activa.</response>
        /// <remarks>
        /// Endpoint publico para iniciar sesion. El front debe enviar codigo de persona y password; si la autenticacion es correcta, la API setea las cookies de access token y refresh token automaticamente.
        /// 
        /// 🔐 Protección DUAL ADAPTATIVA contra fuerza bruta (OWASP Anti-Automation):
        /// 
        /// 1️⃣ Rate Limit por IP SOLAMENTE:
        ///    - Límite: 10 intentos cada 15 minutos por dirección IP
        ///    - Previene: Credential stuffing masivo (probar muchas cuentas)
        ///    - Permite: Múltiples usuarios legítimos en la misma red/PC/hogar
        ///    - Algoritmo: Sliding Window con Redis distribuido
        /// 
        /// 2️⃣ Rate Limit por IP + Documento:
        ///    - Límite: 5 intentos cada 15 minutos por combinación única
        ///    - Previene: Ataque focalizado a una cuenta específica
        ///    - Normaliza formato de documento (ignora puntos/guiones)
        ///    - Bloquea: Intentos repetidos al mismo usuario desde la misma IP
        /// 
        /// 🎯 Ejemplos de comportamiento:
        /// - Usuario olvida contraseña → Bloqueado al 5to intento con SU documento
        /// - Familia con IP compartida → 10 usuarios diferentes pueden intentar (2 veces c/u)
        /// - Ataque a cuenta "12345678" → Bloqueado al 5to intento desde esa IP
        /// - Credential stuffing → Bloqueado al 10mo intento desde esa IP
        /// 
        /// Si cualquiera de los dos límites se excede → HTTP 429 con headers:
        /// - X-RateLimit-Limit: Límite máximo configurado
        /// - X-RateLimit-Remaining: Intentos restantes
        /// - X-RateLimit-Reset: Timestamp UNIX de cuando se resetea
        /// - Retry-After: Segundos hasta poder reintentar
        /// </remarks>
        [AllowAnonymous]
        [EnableRateLimiting("LoginAttempts")]
        [HttpPost("Login")]
        [RequireCaptcha(CaptchaActions.Login, CaptchaValidationMode.ScoreOnly)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoLogin2FARequired>), 202)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 404)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 429)]
        public async Task<IActionResult> Login([FromBody] DtoAuthRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TipoDocumento) || string.IsNullOrWhiteSpace(request.Documento))
            {
                return ValidateResponse(OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "LOGIN_LDAP_01",
                    nameof(Login),
                    "El tipo de documento y el documento son requeridos.",
                    400,
                    default!));
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var recaptchaScore = HttpContext.GetRecaptchaScore();
            if (!recaptchaScore.HasValue)
            {
                return ValidateResponse(OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "AUTH_CAPTCHA_99",
                    nameof(Login),
                    "No se encontro el score de captcha validado.",
                    500,
                    default!));
            }

            var flowResult = await loginFlowService.EjecutarAsync(request, ipAddress, recaptchaScore.Value);

            if (flowResult.RateLimitHeaders != null)
                AgregarHeadersRateLimit(flowResult.RateLimitHeaders);

            if (flowResult.RequiresTwoFactor)
                return ValidateResponse(flowResult.TwoFactorResult!);

            if (flowResult.SetCookies && flowResult.AuthResult?.Data != null)
                SetAuthenticationCookies(flowResult.AuthResult.Data);

            return ValidateResponse(flowResult.AuthResult!);
        }

        private void AgregarHeadersRateLimit(DtoLoginRateLimitHeaders headers)
        {
            WebApiAdmisiones.Extensions.ServiceCollectionExtensions.LoginAccountRateLimitRejections.Inc();
            Response.Headers["X-RateLimit-Limit"] = headers.Limit.ToString();
            Response.Headers["X-RateLimit-Remaining"] = headers.Remaining.ToString();

            if (headers.ResetTime.HasValue)
            {
                Response.Headers["X-RateLimit-Reset"] = headers.ResetTime.Value.ToUnixTimeSeconds().ToString();
                var retryAfter = (int)(headers.ResetTime.Value - DateTimeOffset.UtcNow).TotalSeconds;
                Response.Headers.RetryAfter = Math.Max(0, retryAfter).ToString();
            }
        }

        /// <summary>
        /// Verifica el código de dos factores enviado por email y completa la autenticación.
        /// Los tokens se devuelven como cookies HttpOnly seguras, no en el body de la respuesta.
        /// </summary>
        /// <param name="request">Session ID y código de verificación.</param>
        /// <returns>Resultado de la verificación con información del usuario.</returns>
        /// <response code="200">Verificación exitosa. Las cookies han sido establecidas.</response>
        /// <response code="400">Datos de entrada inválidos.</response>
        /// <response code="401">Código incorrecto, sesión expirada o máximo de intentos superado.</response>
        /// <response code="429">Se superó el máximo de solicitudes de verificación.</response>
        /// <remarks>
        /// Endpoint publico usado como segundo paso del login cuando la API respondio <c>202 Accepted</c> en <c>Auth/Login</c>.
        /// Si el codigo es valido, se eliminan los datos temporales de 2FA y se emiten las cookies normales de autenticacion.
        /// Si el codigo vencio pero la sesion 2FA sigue activa, el front puede solicitar uno nuevo con <c>Auth/ReenviarCodigo2FA</c>.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("VerificarCodigo2FA")]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 429)]
        public async Task<IActionResult> VerificarCodigo2FA([FromBody] DtoVerificarCodigo2FARequest request)
        {
            var result = await dosFactoresService.VerificarCodigoAsync(request.SessionId, request.Codigo);

            if (result.Success && result.Data != null)
            {
                SetAuthenticationCookies(result.Data);
            }

            return ValidateResponse(result);
        }

        /// <summary>
        /// Reenvia el codigo de verificacion de dos factores para una sesion 2FA vigente.
        /// </summary>
        /// <param name="request">Identificador de la sesion 2FA devuelto por <c>Auth/Login</c>.</param>
        /// <returns>Datos necesarios para continuar el login 2FA, incluyendo el mismo sessionId y el email enmascarado.</returns>
        /// <response code="200">Codigo reenviado correctamente. El codigo anterior queda invalidado.</response>
        /// <response code="400">El request es invalido o no contiene sessionId.</response>
        /// <response code="401">La sesion 2FA no existe, expiro o ya fue consumida.</response>
        /// <response code="429">Se supero el limite de reenvios para esta sesion o cuenta.</response>
        /// <remarks>
        /// Endpoint publico para el caso en que el usuario no recibio el mail o el codigo anterior expiro.
        /// Cada reenvio exitoso genera un codigo nuevo, reinicia los intentos de validacion y conserva el tiempo restante de la sesion 2FA.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("ReenviarCodigo2FA")]
        [ProducesResponseType(typeof(OperationResult<DtoLogin2FARequired>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoLogin2FARequired>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoLogin2FARequired>), 401)]
        [ProducesResponseType(typeof(OperationResult<DtoLogin2FARequired>), 429)]
        public async Task<IActionResult> ReenviarCodigo2FA([FromBody] DtoReenviarCodigo2FARequest request)
        {
            var result = await dosFactoresService.ReenviarCodigoAsync(request.SessionId);
            return ValidateResponse(result);
        }


        /// <summary>
        /// Valida el link de creacion y recuperación de password y crea una sesion temporal.
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
        /// Completa la creacion y recuperación de password usando la cookie temporal del link.
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
        [HttpPost("CompletarPassword")]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 404)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 500)]
        public async Task<IActionResult> CompletarPassword([FromBody] DtoCompletarPasswordInicialRequest request)
        {
            var sessionToken = CookieAuthenticationHelper.GetPasswordActivationTokenFromCookie(HttpContext);
            var flowResult = await loginService.CompletarPasswordFlowAsync(sessionToken, request);

            if (flowResult.ClearActivationCookie)
            {
                CookieAuthenticationHelper.ClearPasswordActivationCookie(HttpContext);
            }

            if (flowResult.Result.Success && flowResult.Result.Data != null)
            {
                SetAuthenticationCookies(flowResult.Result.Data);
            }

            return ValidateResponse(flowResult.Result);
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
        [AllowAnonymous]
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
                return ValidateResponse(errorResult);
            }


            // 2. Delegar toda la lógica de negocio al servicio.
            var result = await loginService.RefrescarTokensAsync(refreshToken);

            if (!result.Success)
            {
                // Limpiar cookies si falló la renovación
                CookieAuthenticationHelper.ClearAuthenticationCookies(HttpContext);
                return ValidateResponse(result);
            }

            // 3. Establecer cookies con los nuevos tokens (HTTP concern)
            if (result.Data != null)
            {
                SetAuthenticationCookies(result.Data);
            }

            return ValidateResponse(result);
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
        [AllowAnonymous]
        [RequireCaptcha(CaptchaActions.RecuperarPassword, CaptchaValidationMode.ScoreOnly)]
        [ProducesResponseType(typeof(OperationResult<object>), 200)]
        [ProducesResponseType(typeof(OperationResult<object>), 400)]
        public async Task<IActionResult> RecuperarPassword([FromBody] DtoRecuperarPasswordRequest request)
        {
            var result = await loginService.RecuperarPassword(request);
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
