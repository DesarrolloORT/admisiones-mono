using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
using AppLogic.Integrations.EnrollmentsAndPayments.Interfaces;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.Integrations.EnrollmentsAndPayments.Services;

/// <summary>
/// Cliente tipado para la API de Inscripciones y Pagos.
/// El ServiceAuthenticationHandler inyecta automáticamente los tokens en TODOS los métodos.
/// </summary>
public class EnrollmentsAndPaymentsApiClient(HttpClient httpClient, ILogger<EnrollmentsAndPaymentsApiClient> logger) : IEnrollmentsAndPaymentsApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<EnrollmentsAndPaymentsApiClient> _logger = logger;

    #region Inscripciones

    /// <summary>
    /// Confirma una preinscripción (una o varias ofertas a la vez) en la API de Inscripciones y Pagos.
    /// Corresponde a: POST /ConfirmarPreInscripcionMultiple
    /// </summary>
    public async Task<OperationResult<ConfirmarPreInscripcionMultipleApiResponse>> ConfirmMultiplePreEnrollmentAsync(
        ConfirmarPreInscripcionMultipleApiRequest request)
    {
        return await SendAsync<ConfirmarPreInscripcionMultipleApiResponse>(
            () =>
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                    "Confirmando preinscripción múltiple - Producto: {IdProducto}, Proceso: {IdProceso}, Ofertas: {IdsOfertas}",
                    request.IdProducto,
                    request.IdProceso,
                    string.Join(",", request.IdsOfertasSeleccionadas)
                    );
                }

                var idsQuery = string.Join("&", request.IdsOfertasSeleccionadas.Select(id => $"idsOfertasSeleccionadas={id}"));
                var url = "ORTSecure/Inscripciones/ConfirmarPreInscripcionMultiple"
                    + $"?tipoInscripcion={Uri.EscapeDataString(request.TipoInscripcion)}"
                    + $"&idProducto={request.IdProducto}"
                    + $"&idProceso={request.IdProceso}"
                    + (idsQuery.Length > 0 ? "&" + idsQuery : "");

                return _httpClient.PostAsJsonAsync(url, request.Turno);
            },
            "CONFIRMAR_PREINSCRIPCION_MULTIPLE_01",
            nameof(ConfirmMultiplePreEnrollmentAsync),
            "La API rechazó la confirmación múltiple");
    }

    /// <summary>
    /// Obtiene la seña mínima a pagar de una inscripción existente (read-only).
    /// Corresponde a: GET /Inscripciones/SeniaMinima
    /// </summary>
    public async Task<OperationResult<SeniaMinimaApiResponse>> GetMinimumDepositAsync(long idInscripto, long productId)
    {
        return await SendAsync<SeniaMinimaApiResponse>(
            () =>
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Obteniendo seña mínima - Inscripto: {IdInscripto}, Producto: {IdProducto}", idInscripto, productId);
                }

                var url = $"ORTSecure/Inscripciones/SeniaMinima?idInscripto={idInscripto}&idProducto={productId}";
                return _httpClient.GetAsync(url);
            },
            "SENIA_MINIMA_01",
            nameof(GetMinimumDepositAsync),
            "Error al obtener la seña mínima");
    }

    #endregion

    #region Pagos

    /// <summary>
    /// Obtiene el estado de cuenta corriente.
    /// Corresponde a: GET /api/Pagos/CtaCte
    /// </summary>
    /// <param name="estado">Estado a consultar (ej: "SALDO_ACTUAL_Y_...")</param>
    /// <returns>Información de cuenta corriente</returns>
    public async Task<OperationResult<CtaCteResponse>> GetCurrentAccountAsync(string estado = "SALDO_ACTUAL_Y_MOVIMIENTOS")
    {
        return await SendAsync<CtaCteResponse>(
            () =>
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Consultando cuenta corriente con estado: {Estado}", estado);
                }

                var url = $"ORTSecure/Pagos/CtaCte?estado={Uri.EscapeDataString(estado)}";
                return _httpClient.GetAsync(url);
            },
            "CTACTE_GET_01",
            nameof(GetCurrentAccountAsync),
            "Error al obtener cuenta corriente");
    }

    /// <summary>
    /// Obtiene la lista de cursos pendientes de pago.
    /// Corresponde a: GET /api/Pagos
    /// </summary>
    /// <returns>Lista de cursos con sus montos a pagar</returns>
    public async Task<OperationResult<CursosPagosResponse>> GetCoursePaymentsAsync()
    {
        return await SendAsync<CursosPagosResponse>(
            () =>
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Consultando cursos pendientes de pago");
                }

                return _httpClient.GetAsync("ORTSecure/Pagos/Carritos");
            },
            "CURSOS_PAGOS_GET_01",
            nameof(GetCoursePaymentsAsync),
            "Error al obtener cursos a pagar");
    }

    public async Task<OperationResult<CarritosInscripcionApiResponse>> GetCartsByEnrollmentAsync(IEnumerable<long> idsInscripcion)
    {
        return await SendAsync<CarritosInscripcionApiResponse>(
            () =>
            {
                var idsQuery = string.Join("&", idsInscripcion.Select(id => $"idsInscripcion={id}"));

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Consultando carritos de las inscripciones: {IdsQuery}", idsQuery);
                }

                return _httpClient.GetAsync($"ORTSecure/Pagos/Carritos?{idsQuery}");
            },
            "CARRITOS_INSCRIPCION_GET_01",
            nameof(GetCartsByEnrollmentAsync),
            "Error al obtener carritos de la inscripción");
    }

    /// <summary>Procesa el pago de los carritos de seña de una o varias inscripciones (nivel 3 y 4 con seminarios puede traer más de una) contra la API legacy.</summary>
    public async Task<OperationResult<List<CartPaymentMessage>>> PayCartsByEnrollmentAsync(
        IEnumerable<long> idsInscripcion,
        string paymentType = "PAGO_CUENTA_CORRIENTE")
    {
        return await SendAsync<List<CartPaymentMessage>>(
            () =>
            {
                var idsQuery = string.Join("&", idsInscripcion.Select(id => $"idsInscripcion={id}"));

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                    "Procesando pago de carritos de las inscripciones: {IdsQuery} con tipo de pago: {TipoPago}",
                    idsQuery,
                    paymentType
                    );
                }

                var url = $"ORTSecure/Pagos/Carritos/Pagar?tipoPago={Uri.EscapeDataString(paymentType)}&{idsQuery}";
                return _httpClient.PostAsync(url, null);
            },
            "PAGAR_CARRITOS_01",
            nameof(PayCartsByEnrollmentAsync),
            "La API rechazó el pago");
    }

    public async Task<OperationResult<string>> GetCreateInvoiceUrlByEnrollmentAsync(
        IEnumerable<long> idsInscripcion,
        string paymentType,
        string bank = "")
    {
        return await SendAsync<string>(
            () =>
            {
                var idsQuery = string.Join("&", idsInscripcion.Select(id => $"idsInscripcion={id}"));

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Obteniendo URL de factura de las inscripciones: {IdsQuery} con tipo de pago: {TipoPago}",
                        idsQuery,
                        paymentType);
                }

                var url = $"ORTSecure/Pagos/Carritos/UrlCrearFactura?tipoPago={Uri.EscapeDataString(paymentType)}&banco={Uri.EscapeDataString(bank)}&{idsQuery}";
                return _httpClient.PostAsync(url, null);
            },
            "URL_CREAR_FACTURA_01",
            nameof(GetCreateInvoiceUrlByEnrollmentAsync),
            "La API rechazó la creación de URL de factura");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Ejecuta una llamada HTTP y la traduce a <see cref="OperationResult{T}"/>: éxito deserializa
    /// el body como <typeparamref name="T"/>; falla toma el <c>StatusCode</c> y el body de error;
    /// cualquier excepción se delega a <see cref="HandleException{T}"/>. Equivalente exacto del
    /// patrón try/IsSuccessStatusCode/ReadFromJsonAsync repetido en cada método público.
    /// </summary>
    private Task<OperationResult<T>> SendAsync<T>(
        Func<Task<HttpResponseMessage>> requestFactory,
        string errorCode,
        string operationName,
        string errorMessagePrefix)
        => SendAsync<T, T>(requestFactory, errorCode, operationName, errorMessagePrefix, raw => raw!);

    /// <summary>
    /// Variante de <see cref="SendAsync{T}"/> para métodos que mapean el tipo deserializado
    /// (<typeparamref name="TResponse"/>) a un tipo de resultado distinto (<typeparamref name="TResult"/>).
    /// </summary>
    private async Task<OperationResult<TResult>> SendAsync<TResponse, TResult>(
        Func<Task<HttpResponseMessage>> requestFactory,
        string errorCode,
        string operationName,
        string errorMessagePrefix,
        Func<TResponse?, TResult> map)
    {
        try
        {
            var response = await requestFactory();

            if (response.IsSuccessStatusCode)
            {
                var raw = await response.Content.ReadFromJsonAsync<TResponse>();
                return OperationResult<TResult>.Ok(map(raw), operationName);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return OperationResult<TResult>.IsFailed(
                errorCode,
                operationName,
                $"{errorMessagePrefix}: {response.StatusCode} - {errorContent}",
                (int)response.StatusCode,
                default!);
        }
        catch (Exception ex)
        {
            return HandleException<TResult>(ex, operationName);
        }
    }

    private OperationResult<T> HandleException<T>(Exception ex, string methodName)
    {
        if (ex is TaskCanceledException taskEx && taskEx.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout al comunicarse con API (>2m)");
            return OperationResult<T>.IsFailed("API_TIMEOUT", methodName, "Timeout (2m)", 504, default!);
        }

        if (ex is HttpRequestException)
        {
            _logger.LogError(ex, "Error de red");
            return OperationResult<T>.IsFailed("API_NETWORK", methodName, "Error de red al comunicarse con el servicio.", 503, default!);
        }

        _logger.LogError(ex, "Error inesperado");
        return OperationResult<T>.IsFailed("API_UNEXPECTED", methodName, "Error inesperado al comunicarse con el servicio.", 500, default!);
    }

    #endregion
}
