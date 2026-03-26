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
    public class LoginController(
        ILoginService loginService,
        ILogger<LoginController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<LoginController>(logger, currentUser)
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
        [AllowAnonymous]
        [HttpPost("Login")]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 404)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await loginService.AutenticarUsuarioLDAPAsync(request.CodigoPersona, request.Password);

            if (result.Success && result.Data != null)
            {
                // Establecer los tokens como cookies HttpOnly seguras.
                CookieAuthenticationHelper.SetAccessTokenCookie(HttpContext, result.Data.AccessToken!, 15);
                CookieAuthenticationHelper.SetRefreshTokenCookie(HttpContext, result.Data.RefreshToken!, 7);

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
        [HttpPost("RefreshToken")]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<DtoAuthenticationResponse>), 404)]
        public async Task<IActionResult> RefreshToken()
        {
            // 1. Leer refresh token de la cookie (HTTP concern).
            var refreshToken = CookieAuthenticationHelper.GetRefreshTokenFromCookie(HttpContext);

            // 2. Obtener código de persona del token actual (HTTP concern).
            var codigoPersonaClaim = User.Identity?.Name;

            // 3. Delegar toda la lógica de negocio al servicio.
            var result = await loginService.RefrescarTokensAsync(refreshToken, codigoPersonaClaim);

            if (!result.Success)
            {
                // Limpiar cookies si falló la renovación.
                CookieAuthenticationHelper.ClearAuthenticationCookies(HttpContext);
                return result.HttpCode == 404 ? NotFound(result) : Unauthorized(result);
            }

            // 4. Establecer cookies con los nuevos tokens (HTTP concern).
            if (result.Data != null)
            {
                CookieAuthenticationHelper.SetAccessTokenCookie(HttpContext, result.Data.AccessToken!, 15);
                CookieAuthenticationHelper.SetRefreshTokenCookie(HttpContext, result.Data.RefreshToken!, 7);
            }

            return Ok(result);
        }

        #endregion
    }
}
