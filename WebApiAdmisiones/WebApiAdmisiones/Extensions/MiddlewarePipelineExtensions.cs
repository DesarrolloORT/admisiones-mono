using Microsoft.AspNetCore.HttpOverrides;
using Prometheus;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApiAdmisiones.Security;

namespace WebApiAdmisiones.Extensions
{
    /// <summary>
    /// Métodos de extensión para configurar el pipeline HTTP de la aplicación.
    /// </summary>
    public static class MiddlewarePipelineExtensions
    {
        /// <summary>
        /// Configura el pipeline HTTP con todos los middlewares en el orden correcto.
        /// </summary>
        public static WebApplication ConfigureMiddlewarePipeline(
            this WebApplication app,
            IConfiguration configuration)
        {
            // HSTS en Production, Preproduction y Testing (solo Development lo excluye)
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            // Headers forwarding para reverse proxy
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            // Swagger solo en Development y Testing (NO en Production ni Preproduction)
            if (!app.Environment.IsProductionLike())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAngularApp");
            app.UseRouting();

            // Headers de seguridad
            app.UseSecurityHeaders();

            // Middleware para OPTIONS (CORS preflight)
            app.UseOptionsPreflight();

            // Manejo de excepciones
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Autenticación y Autorización
            app.UseAuthentication();
            app.UseAuthorization();

            // JWT token refresh
            app.UseJwtTokenRefresh(configuration);

            // Model binding error handling
            app.UseMiddleware<ModelBindingErrorLoggingMiddleware>();

            // Métricas Prometheus
            app.UseHttpMetrics();
            app.MapMetrics();

            // Endpoints de la API
            app.MapControllers();

            return app;
        }

        /// <summary>
        /// Agrega headers de seguridad a todas las respuestas.
        /// </summary>
        private static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                context.Response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains; preload";
                context.Response.Headers.XContentTypeOptions = "nosniff";
                context.Response.Headers.XFrameOptions = "DENY";
                context.Response.Headers["Referrer-Policy"] = "no-referrer";
                context.Response.Headers.XXSSProtection = "0";

                context.Response.Headers.ContentSecurityPolicy =
                    "default-src 'self'; " +
                    "script-src 'self'; " +
                    "style-src 'self'; " +
                    "img-src 'self' data:; " +
                    "object-src 'none'; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'";

                context.Response.Headers["Permissions-Policy"] =
                    "geolocation=(), microphone=(), camera=()";

                await next();
            });
        }

        /// <summary>
        /// Maneja requests OPTIONS para CORS preflight.
        /// </summary>
        private static IApplicationBuilder UseOptionsPreflight(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                if (context.Request.Method == HttpMethods.Options)
                {
                    context.Response.StatusCode = 200;
                    await context.Response.CompleteAsync();
                    return;
                }
                await next();
            });
        }

        /// <summary>
        /// Configura la renovación automática de tokens JWT cercanos a expirar.
        /// </summary>
        private static IApplicationBuilder UseJwtTokenRefresh(
            this IApplicationBuilder app,
            IConfiguration configuration)
        {
            return app.Use((ctx, next) =>
            {
                ctx.Response.OnStarting(() =>
                {
                    var user = ctx.User;
                    if (user?.Identity?.IsAuthenticated == true)
                    {
                        var refreshThresholdMinutes = configuration.GetValue<int>("JWT:RefreshThresholdMinutes", 15);

                        if (AuthenticationExtensions.ShouldRefreshToken(user, refreshThresholdMinutes))
                        {
                            var audience = user.FindFirst(JwtRegisteredClaimNames.Aud)?.Value;
                            var issuer = user.FindFirst(JwtRegisteredClaimNames.Iss)?.Value;

                            // Only create token if both issuer and audience are present
                            if (!string.IsNullOrEmpty(issuer) && !string.IsNullOrEmpty(audience))
                            {
                                var identity = (ClaimsIdentity)user.Identity!;

                                var newToken = AuthenticationExtensions.CreateAccessToken(
                                    identity, issuer, audience, TimeSpan.FromMinutes(30));

                                ctx.Response.Headers["x-token"] = newToken;
                                ctx.Response.Headers.AccessControlExposeHeaders = "x-token";
                                ctx.Response.Headers.CacheControl = "no-store";
                            }
                        }
                    }

                    return Task.CompletedTask;
                });

                return next();
            });
        }
    }
}
