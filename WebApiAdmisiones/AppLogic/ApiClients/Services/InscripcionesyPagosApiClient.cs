using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AppLogic.ApiClients.Dtos;
using AppLogic.ApiClients.Interfaces;
using AppLogic.Inscripciones.Responses;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.ApiClients.Services
{
    /// <summary>
    /// Cliente tipado para la API de Inscripciones y Pagos.
    /// El ServiceAuthenticationHandler inyecta automáticamente los tokens en TODOS los métodos.
    /// </summary>
    public class InscripcionesyPagosApiClient : IInscripcionesyPagosApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<InscripcionesyPagosApiClient> _logger;

        public InscripcionesyPagosApiClient(HttpClient httpClient, ILogger<InscripcionesyPagosApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        #region Inscripciones

        /// <summary>
        /// Confirma una preinscripción en la API de Inscripciones y Pagos.
        /// Corresponde a: POST /ConfirmarPreInscripcion
        /// </summary>
        /// <param name="request">Datos de la preinscripción a confirmar</param>
        /// <returns>Resultado de la confirmación</returns>
        public async Task<OperationResult<ConfirmarPreInscripcionApiResponse>> ConfirmarPreInscripcionAsync(
            ConfirmarPreInscripcionApiRequest request)
        {
            return await SendAsync<ConfirmarPreInscripcionApiResponse>(
                () =>
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(
                        "Confirmando preinscripción - Producto: {IdProducto}, Proceso: {IdProceso}, Oferta: {IdOferta}",
                        request.IdProducto,
                        request.IdProceso,
                        request.IdOfertaSeleccionada
                        );
                    }

                    var url = "ORTSecure/Inscripciones/ConfirmarPreInscripcion"
                        + $"?tipoInscripcion={Uri.EscapeDataString(request.TipoInscripcion)}"
                        + $"&idProducto={request.IdProducto}"
                        + $"&idProceso={request.IdProceso}"
                        + $"&idOfertaSeleccionada={request.IdOfertaSeleccionada}";

                    return _httpClient.PostAsJsonAsync(url, request.Turno);
                },
                "CONFIRMAR_PREINSCRIPCION_01",
                nameof(ConfirmarPreInscripcionAsync),
                "La API rechazó la confirmación");
        }

        /// <summary>
        /// Obtiene la seña mínima a pagar de una inscripción existente (read-only).
        /// Corresponde a: GET /Inscripciones/SeniaMinima
        /// </summary>
        public async Task<OperationResult<SeniaMinimaApiResponse>> ObtenerSeniaMinimaAsync(long idInscripto, long idProducto)
        {
            return await SendAsync<SeniaMinimaApiResponse>(
                () =>
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Obteniendo seña mínima - Inscripto: {IdInscripto}, Producto: {IdProducto}", idInscripto, idProducto);
                    }

                    var url = $"ORTSecure/Inscripciones/SeniaMinima?idInscripto={idInscripto}&idProducto={idProducto}";
                    return _httpClient.GetAsync(url);
                },
                "SENIA_MINIMA_01",
                nameof(ObtenerSeniaMinimaAsync),
                "Error al obtener la seña mínima");
        }

        /// <summary>
        /// Obtiene las ofertas disponibles para inscripción en admisiones con proceso.
        /// Corresponde a: GET /OfertasParaInscripcionAdmisionesConProceso
        /// </summary>
        /// <param name="idProducto">ID del producto</param>
        /// <param name="idProceso">ID del proceso</param>
        /// <returns>Lista de ofertas disponibles</returns>
        public async Task<OperationResult<List<OfertaInscripcionDto>>> ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync(
            long idProducto,
            long idProceso)
        {
            return await SendAsync<List<OfertaInscripcionApiResponse>, List<OfertaInscripcionDto>>(
                () =>
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(
                        "Obteniendo ofertas para inscripción con proceso - Producto: {IdProducto}, Proceso: {IdProceso}",
                        idProducto,
                        idProceso
                        );
                    }

                    var url = $"ORTSecure/Inscripciones/OfertasParaInscripcionAdmisionesConProceso?idProducto={idProducto}&idProceso={idProceso}";
                    return _httpClient.GetAsync(url);
                },
                "OFERTAS_INSCRIPCION_PROCESO_01",
                nameof(ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync),
                "Error al obtener ofertas con proceso",
                MapearOfertasInscripcion);
        }

        #endregion

        #region Pagos

        /// <summary>
        /// Obtiene el estado de cuenta corriente.
        /// Corresponde a: GET /api/Pagos/CtaCte
        /// </summary>
        /// <param name="estado">Estado a consultar (ej: "SALDO_ACTUAL_Y_...")</param>
        /// <returns>Información de cuenta corriente</returns>
        public async Task<OperationResult<CtaCteResponse>> ObtenerCtaCteAsync(string estado = "SALDO_ACTUAL_Y_MOVIMIENTOS")
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
                nameof(ObtenerCtaCteAsync),
                "Error al obtener cuenta corriente");
        }

        /// <summary>
        /// Obtiene la lista de cursos pendientes de pago.
        /// Corresponde a: GET /api/Pagos
        /// </summary>
        /// <returns>Lista de cursos con sus montos a pagar</returns>
        public async Task<OperationResult<CursosPagosResponse>> ObtenerCursosPagosAsync()
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
                nameof(ObtenerCursosPagosAsync),
                "Error al obtener cursos a pagar");
        }

        public async Task<OperationResult<CarritosInscripcionApiResponse>> ObtenerCarritosPorInscripcionAsync(long idInscripcion)
        {
            return await SendAsync<CarritosInscripcionApiResponse>(
                () =>
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Consultando carritos de la inscripción: {IdInscripcion}", idInscripcion);
                    }

                    return _httpClient.GetAsync($"ORTSecure/Pagos/Carritos?idInscripcion={idInscripcion}");
                },
                "CARRITOS_INSCRIPCION_GET_01",
                nameof(ObtenerCarritosPorInscripcionAsync),
                "Error al obtener carritos de la inscripción");
        }

        /// <summary>Procesa el pago de los carritos de seña de una inscripción contra la API legacy.</summary>
        public async Task<OperationResult<List<DtoMensajePagoCarrito>>> PagarCarritosPorInscripcionAsync(
            long idInscripcion,
            string tipoPago = "PAGO_CUENTA_CORRIENTE")
        {
            return await SendAsync<List<DtoMensajePagoCarrito>>(
                () =>
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(
                        "Procesando pago de carritos de la inscripción {IdInscripcion} con tipo de pago: {TipoPago}",
                        idInscripcion,
                        tipoPago
                        );
                    }

                    var url = $"ORTSecure/Pagos/Carritos/Pagar?tipoPago={Uri.EscapeDataString(tipoPago)}&idInscripcion={idInscripcion}";
                    return _httpClient.PostAsync(url, null);
                },
                "PAGAR_CARRITOS_01",
                nameof(PagarCarritosPorInscripcionAsync),
                "La API rechazó el pago");
        }


        public async Task<OperationResult<string>> ObtenerUrlCrearFacturaPorInscripcionAsync(
            long idInscripcion,
            string tipoPago,
            string banco = "")
        {
            return await SendAsync<string>(
                () =>
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(
                            "Obteniendo URL de factura de la inscripción {IdInscripcion} con tipo de pago: {TipoPago}",
                            idInscripcion,
                            tipoPago);
                    }

                    var url = $"ORTSecure/Pagos/Carritos/UrlCrearFactura?tipoPago={Uri.EscapeDataString(tipoPago)}&idInscripcion={idInscripcion}&banco={Uri.EscapeDataString(banco)}";
                    return _httpClient.PostAsync(url, null);
                },
                "URL_CREAR_FACTURA_01",
                nameof(ObtenerUrlCrearFacturaPorInscripcionAsync),
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
        /// (<typeparamref name="TResponse"/>) a un tipo de resultado distinto (<typeparamref name="TResult"/>),
        /// como <see cref="ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync"/>.
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

        private static List<OfertaInscripcionDto> MapearOfertasInscripcion(List<OfertaInscripcionApiResponse>? ofertasApi)
        {
            return ofertasApi?
                .Select(MapOfertaInscripcion)
                .ToList() ?? new List<OfertaInscripcionDto>();
        }

        private static OfertaInscripcionDto MapOfertaInscripcion(OfertaInscripcionApiResponse source)
        {
            return new OfertaInscripcionDto
            {
                IdOferta = source.IdOferta,
                Turno = new DtoTurno
                {
                    IdTurno = source.IdTurno,
                    NombreTurno = source.NombreTurno
                },
                HorarioReferencia = source.HorarioReferencia
            };
        }

        private sealed class OfertaInscripcionApiResponse
        {
            public long IdOferta { get; set; }
            public long IdTurno { get; set; }
            public string? NombreTurno { get; set; }
            public string? HorarioReferencia { get; set; }
        }

        #endregion
    }
}
