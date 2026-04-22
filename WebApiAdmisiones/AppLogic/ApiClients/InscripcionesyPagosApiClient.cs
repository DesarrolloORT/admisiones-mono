using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.ApiClients
{
    #region DTOs - Inscripciones

    public class InscripcionRequest
    {
        public long CodigoPersona { get; set; }
        public long CodigoCarrera { get; set; }
        public int Anio { get; set; }
        public string? Observaciones { get; set; }
    }

    public class InscripcionResponse
    {
        public long CodigoInscripcion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public long CodigoPersona { get; set; }
        public long CodigoCarrera { get; set; }
        public int Anio { get; set; }
    }

    public class InscripcionesListResponse
    {
        public List<InscripcionResponse> Inscripciones { get; set; } = new();
        public int TotalCount { get; set; }
    }

    #endregion

    #region DTOs - Pagos

    public class PagoRequest
    {
        public long CodigoInscripcion { get; set; }
        public decimal Monto { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string? Referencia { get; set; }
    }

    public class PagoResponse
    {
        public long CodigoPago { get; set; }
        public long CodigoInscripcion { get; set; }
        public decimal Monto { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaPago { get; set; }
        public string? NumeroComprobante { get; set; }
    }

    public class PagosListResponse
    {
        public List<PagoResponse> Pagos { get; set; } = new();
        public decimal TotalMonto { get; set; }
    }

    #endregion

    /// <summary>
    /// Cliente tipado para la API de Inscripciones y Pagos.
    /// El ServiceAuthenticationHandler inyecta automáticamente los tokens en TODOS los métodos.
    /// </summary>
    public class InscripcionesyPagosApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<InscripcionesyPagosApiClient> _logger;

        public InscripcionesyPagosApiClient(HttpClient httpClient, ILogger<InscripcionesyPagosApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        #region Inscripciones

        public async Task<OperationResult<InscripcionesListResponse>> ObtenerInscripcionesAsync(long codigoPersona)
        {
            try
            {
                _logger.LogInformation("Consultando inscripciones para persona {CodigoPersona}", codigoPersona);
                var response = await _httpClient.GetAsync($"/api/inscripciones?codigoPersona={codigoPersona}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<InscripcionesListResponse>();
                    return OperationResult<InscripcionesListResponse>.Ok(result!, nameof(ObtenerInscripcionesAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<InscripcionesListResponse>.IsFailed(
                    "INSCRIPCIONES_GET_01",
                    nameof(ObtenerInscripcionesAsync),
                    $"Error al obtener inscripciones: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<InscripcionesListResponse>(ex, nameof(ObtenerInscripcionesAsync));
            }
        }

        public async Task<OperationResult<InscripcionResponse>> CrearInscripcionAsync(InscripcionRequest request)
        {
            try
            {
                _logger.LogInformation("Delegando creación de inscripción para persona {CodigoPersona}", request.CodigoPersona);
                var response = await _httpClient.PostAsJsonAsync("/api/inscripciones", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<InscripcionResponse>();
                    return OperationResult<InscripcionResponse>.Ok(result!, nameof(CrearInscripcionAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<InscripcionResponse>.IsFailed(
                    "INSCRIPCIONES_POST_01",
                    nameof(CrearInscripcionAsync),
                    $"La API rechazó la solicitud: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<InscripcionResponse>(ex, nameof(CrearInscripcionAsync));
            }
        }

        #endregion

        #region Pagos

        public async Task<OperationResult<PagosListResponse>> ObtenerPagosAsync(long codigoPersona)
        {
            try
            {
                _logger.LogInformation("Consultando pagos para persona {CodigoPersona}", codigoPersona);
                var response = await _httpClient.GetAsync($"/api/pagos?codigoPersona={codigoPersona}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PagosListResponse>();
                    return OperationResult<PagosListResponse>.Ok(result!, nameof(ObtenerPagosAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<PagosListResponse>.IsFailed(
                    "PAGOS_GET_01",
                    nameof(ObtenerPagosAsync),
                    $"Error al obtener pagos: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<PagosListResponse>(ex, nameof(ObtenerPagosAsync));
            }
        }

        public async Task<OperationResult<PagoResponse>> RegistrarPagoAsync(PagoRequest request)
        {
            try
            {
                _logger.LogInformation("Registrando pago de ${Monto} para inscripción {CodigoInscripcion}", request.Monto, request.CodigoInscripcion);
                var response = await _httpClient.PostAsJsonAsync("/api/pagos", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PagoResponse>();
                    return OperationResult<PagoResponse>.Ok(result!, nameof(RegistrarPagoAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<PagoResponse>.IsFailed(
                    "PAGOS_POST_01",
                    nameof(RegistrarPagoAsync),
                    $"La API rechazó el pago: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<PagoResponse>(ex, nameof(RegistrarPagoAsync));
            }
        }

        #endregion

        #region Helpers

        private OperationResult<T> HandleException<T>(Exception ex, string methodName)
        {
            if (ex is TaskCanceledException taskEx && taskEx.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout al comunicarse con API (>30s)");
                return OperationResult<T>.IsFailed("API_TIMEOUT", methodName, "Timeout (30s)", 504, default!);
            }

            if (ex is HttpRequestException)
            {
                _logger.LogError(ex, "Error de red");
                return OperationResult<T>.IsFailed("API_NETWORK", methodName, $"Error de red: {ex.Message}", 503, default!);
            }

            _logger.LogError(ex, "Error inesperado");
            return OperationResult<T>.IsFailed("API_UNEXPECTED", methodName, $"Error: {ex.Message}", 500, default!);
        }

        #endregion
    }
}
