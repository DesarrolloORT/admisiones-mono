using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Utilities;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Controllers
{
    /// <summary>
    /// Controlador de autenticación: delega toda la lógica a <see cref="IAuthService"/>.
    /// Los tokens se transportan como cookies HttpOnly seguras.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class AuthController(
        IAuthService authService,
        ILogger<AuthController> logger,
        ICurrentUserService currentUser)
        : ApiBaseController<AuthController>(logger, currentUser)
    {
        /// <summary>
        /// Autentica un usuario contra LDAP y establece cookies de access y refresh token.
        /// </summary>
        /// <response code="200">Autenticación exitosa. Las cookies X-Access-Token y X-Refresh-Token han sido establecidas.</response>
        /// <response code="400">Error en los datos de entrada.</response>
        /// <response code="401">Credenciales inválidas.</response>
        /// <response code="404">Usuario no encontrado en base de datos.</response>
        [AllowAnonymous]
        [HttpPost("Login")]
        [ProducesResponseType(typeof(OperationResult<DTOAuthenticationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DTOAuthenticationResponse>), 400)]
        [ProducesResponseType(typeof(OperationResult<DTOAuthenticationResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<DTOAuthenticationResponse>), 404)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await authService.LoginAsync(request);

            if (result.Success && result.Data != null)
            {
                CookieAuthenticationHelper.SetAccessTokenCookie(HttpContext, result.Data.AccessToken!, 15);
                CookieAuthenticationHelper.SetRefreshTokenCookie(HttpContext, result.Data.RefreshToken!, 7);
            }

            return ValidateResponse(result);
        }

        /// <summary>
        /// Cierra la sesión del usuario eliminando las cookies de autenticación.
        /// </summary>
        /// <response code="200">Logout exitoso.</response>
        [HttpPost("Logout")]
        [ProducesResponseType(typeof(OperationResult<string>), 200)]
        public IActionResult Logout()
        {
            CookieAuthenticationHelper.ClearAuthenticationCookies(HttpContext);
            var result = OperationResult<string>.Ok("Sesión cerrada correctamente.", nameof(Logout));
            return ValidateResponse(result);
        }

        /// <summary>
        /// Renueva el access token usando el refresh token almacenado en cookies.
        /// </summary>
        /// <response code="200">Tokens renovados correctamente.</response>
        /// <response code="401">Refresh token inválido, expirado o no encontrado.</response>
        /// <response code="404">Usuario no encontrado en la base de datos.</response>
        [HttpPost("RefreshToken")]
        [ProducesResponseType(typeof(OperationResult<DTOAuthenticationResponse>), 200)]
        [ProducesResponseType(typeof(OperationResult<DTOAuthenticationResponse>), 401)]
        [ProducesResponseType(typeof(OperationResult<DTOAuthenticationResponse>), 404)]
        public async Task<IActionResult> RefreshToken()
        {
            // Leer refresh token de la cookie (HTTP concern)
            var refreshToken = CookieAuthenticationHelper.GetRefreshTokenFromCookie(HttpContext);

            // Obtener código de persona del access token actual (igual que PersonaController en servicios-desaweb)
            var codigoPersonaClaim = ((System.Security.Claims.ClaimsIdentity)User.Identity!).Name;

            var result = await authService.RefrescarTokensAsync(refreshToken, codigoPersonaClaim);

            if (!result.Success)
            {
                CookieAuthenticationHelper.ClearAuthenticationCookies(HttpContext);
                return result.HttpCode == 404 ? NotFound(result) : Unauthorized(result);
            }

            if (result.Data != null)
            {
                CookieAuthenticationHelper.SetAccessTokenCookie(HttpContext, result.Data.AccessToken!, 15);
                CookieAuthenticationHelper.SetRefreshTokenCookie(HttpContext, result.Data.RefreshToken!, 7);
            }

            return Ok(result);
        }
    }
}

