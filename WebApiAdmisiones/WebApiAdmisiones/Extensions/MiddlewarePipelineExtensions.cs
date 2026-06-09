using Microsoft.AspNetCore.HttpOverrides;
using Prometheus;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApiAdmisiones.Observability;
using WebApiAdmisiones.Security.Middleware;

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
            app.UseRateLimiter();  // Rate limiting por endpoint (ver atributo [EnableRateLimiting])
            app.UseAuthorization();

            // Model binding error handling
            app.UseMiddleware<ModelBindingErrorLoggingMiddleware>();

            // Métricas Prometheus
            app.UseHttpMetrics(options =>
            {
                options.AddCustomLabel("client_service", context => ClientTelemetryHeaders.MetricLabel(context, ClientTelemetryHeaders.ClientService));
                options.AddCustomLabel("client_environment", context => ClientTelemetryHeaders.MetricLabel(context, ClientTelemetryHeaders.ClientEnvironment));
                options.AddCustomLabel("client_version", context => ClientTelemetryHeaders.MetricLabel(context, ClientTelemetryHeaders.ClientVersion));
                options.AddCustomLabel("client_device", ClientTelemetryHeaders.ClientDeviceMetricLabel);
                options.AddCustomLabel("client_route", ClientTelemetryHeaders.ClientRouteMetricLabel);
                options.AddCustomLabel("test_run_id", context => ClientTelemetryHeaders.MetricLabel(context, ClientTelemetryHeaders.TestRunId));
            });
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
    }
}
