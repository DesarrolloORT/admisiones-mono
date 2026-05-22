using AppLogic.ApiClients;
using AppLogic.IServices;
using AppLogic.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using WebApiAdmisiones.HttpHandlers;

namespace WebApiAdmisiones.Extensions
{
    /// <summary>
    /// Extensiones para configurar HttpClients con autenticación de servicio.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Configura el cliente HTTP para la API de Inscripciones y Pagos.
        /// SIN políticas de reintento: las inscripciones deben ser operaciones atómicas (funciona o falla, sin duplicados).
        /// </summary>
        public static IServiceCollection AddInscripcionesyPagosApiClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. Registrar el servicio de tokens para APIs internas
            services.AddSingleton<ITokenServiceInternalApi, TokenServiceInternalApi>();

            // 2. Registrar el DelegatingHandler como transient (se crea nuevo para cada request)
            services.AddTransient<ServiceAuthenticationHandler>(sp =>
            {
                var tokenServiceInternalApi = sp.GetRequiredService<ITokenServiceInternalApi>();
                var httpContextAccessor = sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
                return new ServiceAuthenticationHandler(
                    tokenServiceInternalApi,
                    httpContextAccessor,
                    "api-inscripciones-pagos"
                );
            });

            // 3. Configurar HttpClient tipado SIN políticas de reintento
            services.AddHttpClient<InscripcionesyPagosApiClient>(client =>
            {
                var baseUrl = configuration["ApiClients:InscripcionesYPagos:BaseUrl"]
                    ?? throw new InvalidOperationException(
                        "Falta configuración: ApiClients:InscripcionesYPagos:BaseUrl");

                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30); // Timeout claro: 30s máximo
                client.DefaultRequestHeaders.Add("User-Agent", "WebApiAdmisiones/1.0");
            })
            .AddHttpMessageHandler<ServiceAuthenticationHandler>(); // Solo inyecta los tokens, SIN reintentos

            return services;
        }
    }
}
