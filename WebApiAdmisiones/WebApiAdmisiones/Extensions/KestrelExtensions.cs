using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;

namespace WebApiAdmisiones.Extensions
{
    /// <summary>
    /// Métodos de extensión para configurar Kestrel.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class KestrelExtensions
    {
        /// <summary>
        /// Configura opciones de seguridad de Kestrel (TLS, límites de request).
        /// </summary>
        public static IWebHostBuilder ConfigureKestrelSecurity(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.ConfigureKestrel(options =>
            {
                // TLS 1.2 o TLS 1.3 como mínimo
                options.ConfigureHttpsDefaults(listenOptions =>
                {
                    listenOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                });

                // Límite global de request. Cubre archivos binarios de 2 MB enviados como base64 en JSON.
                options.Limits.MaxRequestBodySize = 4 * 1024 * 1024; // 4 MB
            });
        }
    }
}
