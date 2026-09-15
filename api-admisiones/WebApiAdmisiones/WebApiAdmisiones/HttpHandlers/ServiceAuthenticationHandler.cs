using AppLogic.Authentication.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using WebApiAdmisiones.Security.Authentication;
using WebApiAdmisiones.Security.Observability;

namespace WebApiAdmisiones.HttpHandlers
{
    /// <summary>
    /// DelegatingHandler que inyecta automáticamente ambos tokens en las peticiones HTTP:
    /// - Authorization: Bearer {userToken} (del usuario autenticado)
    /// - X-Service-Token: {serviceToken} (identifica a la API llamadora)
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ServiceAuthenticationHandler : DelegatingHandler
    {
        private readonly ITokenServiceInternalApi _tokenServiceInternalApi;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _targetApi;

        /// <summary>
        /// Constructor del handler.
        /// </summary>
        /// <param name="tokenServiceInternalApi">Servicio para generar tokens para APIs internas.</param>
        /// <param name="httpContextAccessor">Accessor para obtener el contexto HTTP actual.</param>
        /// <param name="targetApi">Nombre de la API destino (ej: "api-inscripciones-pagos").</param>
        public ServiceAuthenticationHandler(
            ITokenServiceInternalApi tokenServiceInternalApi,
            IHttpContextAccessor httpContextAccessor,
            string targetApi)
        {
            _tokenServiceInternalApi = tokenServiceInternalApi;
            _httpContextAccessor = httpContextAccessor;
            _targetApi = targetApi;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 1. Obtener el access token del usuario actual (desde cookies)
            var userAccessToken = CookieAuthenticationHelper.GetAccessTokenFromCookie(
                _httpContextAccessor.HttpContext!
            );

            if (!string.IsNullOrEmpty(userAccessToken))
            {
                // Agregar el token del usuario (Bearer)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAccessToken);
            }

            // 2. Generar y agregar el service token
            var scopes = new[] { "inscripciones.write", "pagos.read" }; // Configurar según necesidad
            var serviceToken = _tokenServiceInternalApi.GenerateServiceToken(_targetApi, scopes);
            request.Headers.Add("X-Service-Token", serviceToken);

            // 3. Agregar headers adicionales para trazabilidad
            request.Headers.Add("X-Source-Service", "api-admisiones");
            request.Headers.Add("X-Correlation-Id", LoggingHelper.EnsureCorrelationId(_httpContextAccessor.HttpContext).ToString());

            // 4. Enviar la petición
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
