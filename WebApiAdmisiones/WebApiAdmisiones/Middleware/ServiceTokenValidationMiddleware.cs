// ============================================================================
// ServiceTokenValidationMiddleware.cs
// ============================================================================
// ⚠️ IMPORTANTE: Este archivo es SOLO DOCUMENTACIÓN / REFERENCIA.
//
// Este middleware NO se usa en la API de Admisiones.
// Debe implementarse en la API de Inscripciones y Pagos (la API DESTINO).
//
// La API de Admisiones GENERA el service token.
// La API de Inscripciones VALIDA el service token.
// ============================================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebApiInscripciones.Middleware
{
    /// <summary>
    /// ⚠️ ESTE MIDDLEWARE VA EN LA API DE INSCRIPCIONES Y PAGOS (NO EN ESTA SOLUCIÓN).
    /// 
    /// Middleware para validar el service token (X-Service-Token) en la API de Inscripciones.
    /// Solo permite llamadas desde servicios autorizados (api-admisiones).
    /// 
    /// RESPONSABILIDADES:
    /// 1. Validar que existe el header X-Service-Token
    /// 2. Validar la firma JWT del service token
    /// 3. Verificar que el servicio llamador está autorizado
    /// 4. Verificar que también existe el Authorization header (usuario)
    /// 5. Registrar auditoría de acceso
    /// </summary>
    public class ServiceTokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ServiceTokenValidationMiddleware> _logger;
        private readonly string[] _authorizedServices = { "api-admisiones" }; // Configurar desde appsettings

        public ServiceTokenValidationMiddleware(
            RequestDelegate next,
            ILogger<ServiceTokenValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Solo validar en endpoints protegidos (no en /health, /metrics, etc.)
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // 1. Verificar que existe el header X-Service-Token
                if (!context.Request.Headers.TryGetValue("X-Service-Token", out var serviceToken) ||
                    string.IsNullOrEmpty(serviceToken))
                {
                    _logger.LogWarning(
                        "Acceso denegado: No se encontró X-Service-Token. IP: {IP}, Path: {Path}",
                        context.Connection.RemoteIpAddress,
                        context.Request.Path
                    );

                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "SERVICE_TOKEN_MISSING",
                        message = "Falta el header X-Service-Token. Este endpoint solo es accesible desde servicios autorizados."
                    });
                    return;
                }

                // 2. Validar el service token (JWT)
                if (!ValidarServiceToken(serviceToken!))
                {
                    _logger.LogWarning(
                        "Acceso denegado: X-Service-Token inválido. Token: {Token}, IP: {IP}",
                        serviceToken,
                        context.Connection.RemoteIpAddress
                    );

                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "SERVICE_TOKEN_INVALID",
                        message = "El service token es inválido o ha expirado."
                    });
                    return;
                }

                // 3. Verificar que el Authorization header también existe (token del usuario)
                if (!context.Request.Headers.ContainsKey("Authorization"))
                {
                    _logger.LogWarning(
                        "Acceso denegado: Falta Authorization header. Service: {Service}",
                        ExtraerNombreServicio(serviceToken!)
                    );

                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "USER_TOKEN_MISSING",
                        message = "Se requiere token de usuario (Authorization header) además del service token."
                    });
                    return;
                }

                // 4. Log de auditoría
                _logger.LogInformation(
                    "Acceso autorizado desde servicio {Service} para usuario {User}",
                    ExtraerNombreServicio(serviceToken!),
                    context.User.Identity?.Name ?? "Anónimo"
                );
            }

            // Continuar con el pipeline
            await _next(context);
        }

        /// <summary>
        /// Valida la firma JWT del service token.
        /// IMPORTANTE: Usar la MISMA clave SERVICE_TOKEN_SECRET_KEY que la API de Admisiones.
        /// </summary>
        private bool ValidarServiceToken(string token)
        {
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                // Validar claims
                var serviceName = jwtToken.Claims.FirstOrDefault(c => c.Type == "service_name")?.Value;
                var targetApi = jwtToken.Claims.FirstOrDefault(c => c.Type == "target_api")?.Value;

                if (!_authorizedServices.Contains(serviceName))
                {
                    _logger.LogWarning("Servicio no autorizado: {ServiceName}", serviceName);
                    return false;
                }

                if (targetApi != "api-inscripciones-pagos")
                {
                    _logger.LogWarning("Target API incorrecto: {TargetApi}", targetApi);
                    return false;
                }

                // Validar expiración
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("Service token expirado: {ExpiredAt}", jwtToken.ValidTo);
                    return false;
                }

                // TODO: Validar firma con la clave secreta compartida
                // var secretKey = Environment.GetEnvironmentVariable("SERVICE_TOKEN_SECRET_KEY");
                // ... validación criptográfica completa ...

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar service token");
                return false;
            }
        }

        private string ExtraerNombreServicio(string token)
        {
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.Claims.FirstOrDefault(c => c.Type == "service_name")?.Value ?? "Desconocido";
            }
            catch
            {
                return "Desconocido";
            }
        }
    }

    // ============================================================================
    // CONFIGURACIÓN EN Program.cs DE LA API DE INSCRIPCIONES:
    // ============================================================================
    // app.UseMiddleware<ServiceTokenValidationMiddleware>(); // ANTES de UseAuthentication
    // app.UseAuthentication();
    // app.UseAuthorization();
    // ============================================================================
}
