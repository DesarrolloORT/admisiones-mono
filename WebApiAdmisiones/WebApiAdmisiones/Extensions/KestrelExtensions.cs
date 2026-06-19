using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;

namespace WebApiAdmisiones.Extensions
{
    /// <summary>
    /// Metodos de extension para configurar Kestrel.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class KestrelExtensions
    {
        private const long MaxJsonUploadRequestBodySize = 16 * 1024 * 1024;

        /// <summary>
        /// Configura opciones de seguridad de Kestrel (TLS, limites de request).
        /// </summary>
        public static IWebHostBuilder ConfigureKestrelSecurity(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.ConfigureKestrel(options =>
            {
                // TLS 1.2 o TLS 1.3 como minimo.
                options.ConfigureHttpsDefaults(listenOptions =>
                {
                    listenOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                });

                // Los uploads viajan como base64 en JSON y crecen aproximadamente 4/3.
                // Este limite permite los documentos de 10 MB validados por FileValidationHelper.
                options.Limits.MaxRequestBodySize = MaxJsonUploadRequestBodySize;
            });
        }
    }
}
