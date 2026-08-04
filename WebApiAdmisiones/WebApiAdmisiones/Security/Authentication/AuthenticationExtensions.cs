using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebApiAdmisiones.Security.Authentication
{
    /// <summary>
    /// Proporciona métodos de extensión para configurar la autenticación JWT en la aplicación.
    /// </summary>
    public static class AuthenticationExtensions
    {
        private const string IssuerAdmisiones = "https://admisiones.ort.edu.uy";
        private const int MinimumKeyLengthBytes = 32; // HS256 requiere >= 256 bits

        /// <summary>
        /// Agrega y configura la autenticación JWT Bearer al contenedor de servicios.
        /// </summary>
        /// <param name="services">La colección de servicios de la aplicación.</param>
        /// <param name="configuration">La configuración de la aplicación.</param>
        /// <returns>La colección de servicios con la autenticación configurada.</returns>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        /// <summary>
                        /// Indica si se debe validar el emisor del token.
                        /// </summary>
                        ValidateIssuer = true,
                        /// <summary>
                        /// Indica si se debe validar la audiencia del token.
                        /// </summary>
                        ValidateAudience = true,
                        /// <summary>
                        /// Indica si se debe validar la vigencia del token.
                        /// </summary>
                        ValidateLifetime = true,
                        /// <summary>
                        /// Indica si se debe validar la clave de firma del emisor.
                        /// </summary>
                        ValidateIssuerSigningKey = true,
                        /// <summary>
                        /// Lista de audiencias válidas para el token.
                        /// </summary>
                        ValidAudiences =
                        [
                            IssuerAdmisiones
                        ],
                        /// <summary>
                        /// Lista de emisores válidos para el token.
                        /// </summary>
                        ValidIssuers =
                        [
                            IssuerAdmisiones
                        ],

                        /// <summary>
                        /// Permite resolver la clave de firma según el emisor del token.
                        /// </summary>
                        IssuerSigningKeyResolver = ResolveIssuerSigningKey,
                        /// <summary>
                        /// Restringe los algoritmos aceptados para evitar confusión de algoritmos.
                        /// </summary>
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                        /// <summary>
                        /// Tolerancia de reloj entre servidores (el default de la librería es 5 minutos).
                        /// </summary>
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };

                    /// <summary>
                    /// Configura los eventos del ciclo de vida de la autenticación JWT.
                    /// </summary>
                    options.Events = new JwtBearerEvents
                    {
                        /// <summary>
                        /// Evento que se ejecuta cuando se recibe un mensaje con token.
                        /// Prioriza la lectura del token desde cookies HttpOnly (más seguro).
                        /// </summary>
                        OnMessageReceived = HandleOnMessageReceived,
                        /// <summary>
                        /// Evento que se ejecuta cuando el token es validado correctamente.
                        /// </summary>
                        OnTokenValidated = HandleOnTokenValidated,
                        /// <summary>
                        /// Evento que se ejecuta cuando falla la autenticación.
                        /// </summary>
                        OnAuthenticationFailed = HandleOnAuthenticationFailed
                    };
                });

            return services;
        }

        private static SymmetricSecurityKey[] ResolveIssuerSigningKey(
            string token,
            SecurityToken securityToken,
            string kid,
            TokenValidationParameters parameters)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var issuer = jwtToken?.Issuer;

            var keysByIssuer = new Dictionary<string, string?>
            {
                { IssuerAdmisiones, Environment.GetEnvironmentVariable("JWT_SECRET_KEY") }
            };

            if (issuer != null && keysByIssuer.TryGetValue(issuer, out var secret))
            {
                if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < MinimumKeyLengthBytes)
                {
                    throw new InvalidOperationException(
                        $"JWT_SECRET_KEY debe estar configurada con al menos {MinimumKeyLengthBytes} bytes para HS256.");
                }

                return new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)) };
            }

            throw new SecurityTokenInvalidIssuerException("Issuer no autorizado.");
        }

        private static Task HandleOnMessageReceived(MessageReceivedContext context)
        {
            // PRIORIDAD 1: Intentar obtener el token desde la cookie HttpOnly (RECOMENDADO)
            var tokenFromCookie = CookieAuthenticationHelper.GetAccessTokenFromCookie(context.HttpContext);

            if (!string.IsNullOrEmpty(tokenFromCookie))
            {
                context.Token = tokenFromCookie;
                return Task.CompletedTask;
            }

            // PRIORIDAD 2: Fallback al header Authorization (para compatibilidad con APIs externas)
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = authHeader.Substring("Bearer ".Length).Trim();
            }

            return Task.CompletedTask;
        }

        private static Task HandleOnTokenValidated(TokenValidatedContext context)
        {
            return Task.CompletedTask;
        }

        private static Task HandleOnAuthenticationFailed(AuthenticationFailedContext context)
        {
            return Task.CompletedTask;
        }
    }
}
