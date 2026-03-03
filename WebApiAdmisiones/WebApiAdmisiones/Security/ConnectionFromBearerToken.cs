using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace WebApiAdmisiones.Security
{
    /// <summary>
    /// Proporciona la lógica para obtener la cadena de conexión adecuada
    /// a partir del token JWT presente en la cabecera Authorization.
    /// </summary>
    public class ConnectionFromBearerToken
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ConnectionFromBearerToken"/>.
        /// </summary>
        /// <param name="httpContextAccessor">Acceso al contexto HTTP actual.</param>
        /// <param name="configuration">Configuración de la aplicación.</param>
        public ConnectionFromBearerToken(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        /// <summary>
        /// Obtiene la cadena de conexión correspondiente según el issuer del token JWT.
        /// </summary>
        /// <returns>La cadena de conexión seleccionada o vacía si no se encuentra un token válido.</returns>
        public string GetConnectionString()
        {
            var token = _httpContextAccessor.HttpContext?
                .Request.Headers.Authorization
                .ToString()
                .Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var issuerClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "iss")?.Value ?? "";
                return GetConnectionStringFromToken(issuerClaim);
            }

            // Fallback: sin token, usar variable de entorno por defecto
            return GetConnectionStringFromToken(string.Empty);
        }

        /// <summary>
        /// Devuelve la cadena de conexión según el valor del issuer extraído del token.
        /// </summary>
        /// <param name="issuerClaim">El valor del claim 'iss' del token JWT.</param>
        /// <returns>La cadena de conexión correspondiente al issuer.</returns>
        private string GetConnectionStringFromToken(string issuerClaim)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return issuerClaim switch
            {
                null or "" => Environment.GetEnvironmentVariable("OracleConnectionString"),
                "https://funcionarios.ort.edu.uy" => Environment.GetEnvironmentVariable("OracleConnectionStringFuncionarios"),
                "https://gestion.ort.edu.uy" => Environment.GetEnvironmentVariable("OracleConnectionStringGestion"),
                "https://admisiones.ort.edu.uy" => Environment.GetEnvironmentVariable("OracleConnectionStringAdmisiones"),
                _ => _configuration["OracleConnectionString"]
            };
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}
