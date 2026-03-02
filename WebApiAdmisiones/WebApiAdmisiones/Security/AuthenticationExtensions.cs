using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebApiAdmisiones.Security
{
    /// <summary>
    /// Proporciona métodos de extensión para configurar la autenticación JWT en la aplicación.
    /// </summary>
    public static class AuthenticationExtensions
    {
        private const string IssuerGestion = "https://gestion.ort.edu.uy";
        private const string IssuerFuncionarios = "https://funcionarios.ort.edu.uy";
        private const string IssuerAdmisiones = "https://admisiones.ort.edu.uy";

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
                            IssuerGestion,
                            IssuerFuncionarios,
                            IssuerAdmisiones
                        ],
                        /// <summary>
                        /// Lista de emisores válidos para el token.
                        /// </summary>
                        ValidIssuers =
                        [
                            IssuerGestion,
                            IssuerFuncionarios,
                            IssuerAdmisiones
                        ],

                        /// <summary>
                        /// Permite resolver la clave de firma según el emisor del token.
                        /// </summary>
                        IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                        {
                            var handler = new JwtSecurityTokenHandler();
                            var jwtToken = handler.ReadJwtToken(token);
                            var issuer = jwtToken?.Issuer;

                            // 🔍 DEBUG: podés poner un breakpoint acá
                            Console.WriteLine($"Token recibido - issuer: {issuer}");

                            // Ejemplo de múltiples claves por issuer
                            var keysByIssuer = new Dictionary<string, string?>
                            {
                                { IssuerGestion, Environment.GetEnvironmentVariable("JWT_Key_Gestion") },
                                { IssuerFuncionarios, Environment.GetEnvironmentVariable("JWT_Key_Funcionarios") },
                                { IssuerAdmisiones, Environment.GetEnvironmentVariable("JWT_Key_Admisiones") },
                            };

                            if (issuer != null && keysByIssuer.TryGetValue(issuer, out var secret))
                            {
                                var keyBytes = Array.Empty<byte>();
                                if (secret != null)
                                {
                                    keyBytes = Encoding.UTF8.GetBytes(secret);
                                }
                                return new[] { new SymmetricSecurityKey(keyBytes) };
                            }

                            // Si no encontrás la clave, devolvé vacío o tirá excepción
                            throw new SecurityTokenInvalidIssuerException("Issuer no autorizado.");
                        }
                    };

                    /// <summary>
                    /// Configura los eventos del ciclo de vida de la autenticación JWT.
                    /// </summary>
                    options.Events = new JwtBearerEvents
                    {
                        /// <summary>
                        /// Evento que se ejecuta cuando se recibe un mensaje con token.
                        /// </summary>
                        OnMessageReceived = context =>
                        {
                            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

                            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = authHeader.Substring("Bearer ".Length).Trim();
                                Console.WriteLine($"Token extraído manualmente: {context.Token}");
                            }
                            else
                            {
                                Console.WriteLine("No se encontró Authorization o no empieza con 'Bearer '");
                            }

                            return Task.CompletedTask;
                        },
                        /// <summary>
                        /// Evento que se ejecuta cuando el token es validado correctamente.
                        /// </summary>
                        OnTokenValidated = context =>
                        {
                            Console.WriteLine("Token validado correctamente.");
                            return Task.CompletedTask;
                        },
                        /// <summary>
                        /// Evento que se ejecuta cuando falla la autenticación.
                        /// </summary>
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"Error de autenticación: {context.Exception.Message}");
                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        /// <summary>
        /// Crea un token de acceso JWT con las claims proporcionadas.
        /// </summary>
        /// <param name="identity">La identidad con las claims a incluir en el token.</param>
        /// <param name="issuer">El emisor del token.</param>
        /// <param name="audience">La audiencia del token.</param>
        /// <param name="ttl">Tiempo de vida del token.</param>
        /// <returns>Token JWT firmado.</returns>
        public static string CreateAccessToken(ClaimsIdentity identity, string issuer, string audience, TimeSpan ttl)
        {
            var secret = string.Empty;

            switch (issuer)
            {
                case IssuerGestion:
                    secret = Environment.GetEnvironmentVariable("JWT_Key_Gestion");
                    if (string.IsNullOrEmpty(secret))
                    {
                        throw new InvalidOperationException("JWT secret for 'gestion' is not set. Please configure the 'JWT_Key_Gestion' environment variable.");
                    }
                    break;
                case IssuerFuncionarios:
                    secret = Environment.GetEnvironmentVariable("JWT_Key_Funcionarios");
                    if (string.IsNullOrEmpty(secret))
                    {
                        throw new InvalidOperationException("JWT secret for 'funcionarios' is not set. Please configure the 'JWT_Key_Funcionarios' environment variable.");
                    }
                    break;
                case IssuerAdmisiones:
                    secret = Environment.GetEnvironmentVariable("JWT_Key_Admisiones");
                    if (string.IsNullOrEmpty(secret))
                    {
                        throw new InvalidOperationException("JWT secret for 'admisiones' is not set. Please configure the 'JWT_Key_Admisiones' environment variable.");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown issuer '{issuer}'. Supported issuers are: {IssuerGestion}, {IssuerFuncionarios}, {IssuerAdmisiones}");
            }

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                SecurityAlgorithms.HmacSha256);


            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: identity.Claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.Add(ttl),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Determina si un token necesita ser renovado basándose en su tiempo de expiración.
        /// </summary>
        /// <param name="user">El usuario autenticado con los claims del token.</param>
        /// <param name="refreshThresholdMinutes">Umbral en minutos antes de la expiración para renovar el token. Por defecto 15 minutos.</param>
        /// <returns>True si el token debe ser renovado, false en caso contrario.</returns>
        public static bool ShouldRefreshToken(ClaimsPrincipal user, int refreshThresholdMinutes = 15)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var expClaim = user.FindFirst(JwtRegisteredClaimNames.Exp);
            if (expClaim == null || !long.TryParse(expClaim.Value, out var exp))
                return false;

            // Convertir Unix timestamp a DateTime
            var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            var currentTime = DateTime.UtcNow;
            var timeUntilExpiration = expirationTime - currentTime;

            // Renovar si queda menos tiempo que el umbral especificado
            return timeUntilExpiration.TotalMinutes <= refreshThresholdMinutes;
        }

    }
}
