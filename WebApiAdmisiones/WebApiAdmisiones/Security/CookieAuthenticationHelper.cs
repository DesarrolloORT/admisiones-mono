using Microsoft.AspNetCore.Http;

namespace WebApiAdmisiones.Security
{
    /// <summary>
    /// Helper para configurar cookies de autenticación de forma segura.
    /// </summary>
    public static class CookieAuthenticationHelper
    {
        /// <summary>Nombre de la cookie del access token.</summary>
        public const string AccessTokenCookieName = "X-Access-Token";

        /// <summary>Nombre de la cookie del refresh token.</summary>
        public const string RefreshTokenCookieName = "X-Refresh-Token";

        /// <summary>
        /// Establece el access token como una cookie HttpOnly segura.
        /// </summary>
        public static void SetAccessTokenCookie(HttpContext context, string accessToken, int expiresInMinutes = 15)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,   // false en HTTP local, true en producción HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes),
                Path = "/",
                IsEssential = true
            };

            context.Response.Cookies.Append(AccessTokenCookieName, accessToken, cookieOptions);
        }

        /// <summary>
        /// Establece el refresh token como una cookie HttpOnly segura.
        /// </summary>
        public static void SetRefreshTokenCookie(HttpContext context, string refreshToken, int expiresInDays = 7)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,   // false en HTTP local, true en producción HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(expiresInDays),
                Path = "/",
                IsEssential = true
            };

            context.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
        }

        /// <summary>Obtiene el access token desde la cookie.</summary>
        public static string? GetAccessTokenFromCookie(HttpContext context)
            => context.Request.Cookies[AccessTokenCookieName];

        /// <summary>Obtiene el refresh token desde la cookie.</summary>
        public static string? GetRefreshTokenFromCookie(HttpContext context)
            => context.Request.Cookies[RefreshTokenCookieName];

        /// <summary>Elimina las cookies de autenticación (logout).</summary>
        public static void ClearAuthenticationCookies(HttpContext context)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            };

            context.Response.Cookies.Delete(AccessTokenCookieName, options);
            context.Response.Cookies.Delete(RefreshTokenCookieName, options);
        }
    }
}
