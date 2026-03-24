using System.Security.Authentication;

namespace WebApiFDP.Extensions
{
    /// <summary>
    /// Métodos de extensión para configurar Kestrel.
    /// </summary>
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

                // Límite global de tamaño de request (protección DoS)
                options.Limits.MaxRequestBodySize = 2 * 1024 * 1024; // 2 MB
            });
        }
    }
}
