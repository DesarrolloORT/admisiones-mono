using Microsoft.AspNetCore.Http;

namespace WebApiAdmisiones.Security.Authentication
{
    /// <summary>
    /// Helper para configurar cookies de autenticación de forma segura.
    /// </summary>
    public static class CookieAuthenticationHelper
    {
        /// <summary>
        /// Nombre de la cookie del access token.
        /// </summary>
        public const string AccessTokenCookieName = "X-Access-Token";

        /// <summary>
        /// Nombre de la cookie del refresh token.
        /// </summary>
        public const string RefreshTokenCookieName = "X-Refresh-Token";

        /// <summary>
        /// Nombre de la cookie temporal para completar la password inicial.
        /// </summary>
        public const string PasswordActivationCookieName = "X-Password-Activation";

        /// <summary>
        /// Dominio para las cookies de auth. Vacío/no seteado = host-only (local, sobre localhost).
        /// En ambientes desplegados setear AUTH_COOKIE_DOMAIN=.ort.edu.uy para que la cookie
        /// se comparta entre subdominios (admisiones y FDP).
        /// </summary>
        private static string? CookieDomain()
        {
            var domain = Environment.GetEnvironmentVariable("AUTH_COOKIE_DOMAIN");
            return string.IsNullOrWhiteSpace(domain) ? null : domain;
        }

        /// <summary>
        /// Strict en ambientes productivos (Production/Preproduction, ver EnvironmentExtensions.IsProductionLike);
        /// None en el resto, donde front y API suelen correr en orígenes distintos (localhost:4200, etc.).
        /// </summary>
        private static SameSiteMode ResolveSameSite()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isProductionLike = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase)
                || string.Equals(environmentName, "Preproduction", StringComparison.OrdinalIgnoreCase);

            return isProductionLike ? SameSiteMode.Strict : SameSiteMode.None;
        }

        /// <summary>
        /// Establece el access token como una cookie HttpOnly segura.
        /// </summary>
        /// <param name="context">Contexto HTTP.</param>
        /// <param name="accessToken">Token de acceso JWT.</param>
        /// <param name="expiresInMinutes">Minutos hasta la expiración (por defecto 15).</param>
        public static void SetAccessTokenCookie(HttpContext context, string accessToken, int expiresInMinutes = 15)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,   // false en HTTP local, true en producción HTTPS
                SameSite = ResolveSameSite(),
                Expires = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes),
                Path = "/",
                Domain = CookieDomain(),
                IsEssential = true
            };

            context.Response.Cookies.Append(AccessTokenCookieName, accessToken, cookieOptions);
        }

        /// <summary>
        /// Establece el refresh token como una cookie HttpOnly segura.
        /// </summary>
        /// <param name="context">Contexto HTTP.</param>
        /// <param name="refreshToken">Token de refresco.</param>
        /// <param name="expiresInDays">Días hasta la expiración (por defecto 7).</param>
        public static void SetRefreshTokenCookie(HttpContext context, string refreshToken, int expiresInDays = 7)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,   // false en HTTP local, true en producción HTTPS
                SameSite = ResolveSameSite(),
                Expires = DateTimeOffset.UtcNow.AddDays(expiresInDays),
                Path = "/",
                Domain = CookieDomain(),
                IsEssential = true
            };

            context.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
        }

        /// <summary>
        /// Obtiene el access token desde la cookie.
        /// </summary>
        /// <param name="context">Contexto HTTP.</param>
        /// <returns>Access token o null si no existe.</returns>
        public static string? GetAccessTokenFromCookie(HttpContext context)
        {
            return context.Request.Cookies[AccessTokenCookieName];
        }

        /// <summary>
        /// Obtiene el refresh token desde la cookie.
        /// </summary>
        /// <param name="context">Contexto HTTP.</param>
        /// <returns>Refresh token o null si no existe.</returns>
        public static string? GetRefreshTokenFromCookie(HttpContext context)
        {
            return context.Request.Cookies[RefreshTokenCookieName];
        }

        /// <summary>
        /// Establece el token temporal de activacion como una cookie HttpOnly segura.
        /// </summary>
        public static void SetPasswordActivationCookie(HttpContext context, string activationToken, int expiresInMinutes = 15)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = ResolveSameSite(),
                Expires = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes),
                Path = "/",
                Domain = CookieDomain(),
                IsEssential = true
            };

            context.Response.Cookies.Append(PasswordActivationCookieName, activationToken, cookieOptions);
        }

        /// <summary>
        /// Obtiene el token temporal de activacion desde la cookie.
        /// </summary>
        public static string? GetPasswordActivationTokenFromCookie(HttpContext context)
        {
            return context.Request.Cookies[PasswordActivationCookieName];
        }

        /// <summary>
        /// Elimina la cookie temporal de activacion.
        /// </summary>
        public static void ClearPasswordActivationCookie(HttpContext context)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = ResolveSameSite(),
                Path = "/",
                Domain = CookieDomain()
            };

            context.Response.Cookies.Delete(PasswordActivationCookieName, options);
        }

        /// <summary>
        /// Elimina las cookies de autenticación (logout).
        /// </summary>
        /// <param name="context">Contexto HTTP.</param>
        public static void ClearAuthenticationCookies(HttpContext context)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = ResolveSameSite(),
                Path = "/",
                Domain = CookieDomain()
            };

            context.Response.Cookies.Delete(AccessTokenCookieName, options);
            context.Response.Cookies.Delete(RefreshTokenCookieName, options);
            context.Response.Cookies.Delete(PasswordActivationCookieName, options);
        }
    }
}
