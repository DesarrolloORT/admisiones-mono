using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AppLogic.Dtos.Inscripciones;
using Microsoft.Extensions.Logging;
using Utilities;

namespace AppLogic.ApiClients
{
    #region DTOs - Inscripciones

    /// <summary>
    /// DTO para turno (usado en confirmar preinscripción).
    /// </summary>
    public class DtoTurno
    {
        public long IdTurno { get; set; }
        public string? NombreTurno { get; set; }
    }

    /// <summary>
    /// Request para confirmar una preinscripción.
    /// Corresponde a: POST /ConfirmarPreInscripcion
    /// </summary>
    public class ConfirmarPreInscripcionApiRequest
    {
        public DtoTurno Turno { get; set; } = new();
        public string TipoInscripcion { get; set; } = string.Empty;
        public long IdProducto { get; set; }
        public long IdProceso { get; set; }
        public long IdOfertaSeleccionada { get; set; }
    }

    /// <summary>
    /// Response de confirmar preinscripción.
    /// </summary>
    public class ConfirmarPreInscripcionApiResponse
    {
        public bool Confirmada { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long? IdInscripcion { get; set; }
        public DateTime? FechaVencimientoPago { get; set; }
        public List<CarritoSeniaApiDto> Carritos { get; set; } = new();
        public ResumenInscripcionApiDto? Resumen { get; set; }
        public EstadoCuentaApiDto? EstadoCuenta { get; set; }
    }

    public class CarritoSeniaApiDto
    {
        public string IdCarrito { get; set; } = string.Empty;
        public decimal Senia { get; set; }
    }

    public class ResumenInscripcionApiDto
    {
        public long IdOferta { get; set; }
        public long IdProducto { get; set; }
        public string? Carrera { get; set; }
        public long IdComienzo { get; set; }
        public string? Comienzo { get; set; }
        public long IdTurno { get; set; }
        public string? Turno { get; set; }
    }

    public class EstadoCuentaApiDto
    {
        public decimal SaldoActual { get; set; }
    }


    /// <summary>
    /// Response de la seña mínima a pagar de una inscripción (read-only).
    /// Corresponde a: GET /Inscripciones/SeniaMinima
    /// </summary>
    public class SeniaMinimaApiResponse
    {
        public decimal SeniaMinima { get; set; }
    }

    /// <summary>
    /// DTO para oferta de inscripción (usado en los GET de ofertas).
    /// </summary>
    public class OfertaInscripcionDto
    {
        public long IdOferta { get; set; }
        public DtoTurno Turno { get; set; } = new();
        public string? HorarioReferencia { get; set; }
    }

    #endregion

    #region DTOs - Pagos

    /// <summary>
    /// DTO para par clave-valor usado en carritos de pago.
    /// Usado en: POST /Pagos/Carritos/Pagar y POST /Carritos/UrlCrearFactura
    /// </summary>
    public class ClaveValorCarrito
    {
        public string Clave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string ClaveCarrito { get; set; } = string.Empty;
        public string CantidadCuotasAPagar { get; set; } = string.Empty;
        public string Banco { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response de cuenta corriente.
    /// Corresponde a: GET /api/Pagos/CtaCte
    /// </summary>
    public class CtaCteResponse
    {
        public decimal SaldoActual { get; set; }
        public decimal SaldoVencido { get; set; }
        public decimal SaldoAVencer { get; set; }
        public List<MovimientoCtaCte> Movimientos { get; set; } = new();
    }

    /// <summary>
    /// Movimiento de cuenta corriente.
    /// </summary>
    public class MovimientoCtaCte
    {
        public DateTime Fecha { get; set; }
        public string? Concepto { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        public decimal Saldo { get; set; }
    }

    /// <summary>
    /// Response de cursos a pagar.
    /// Corresponde a: GET /api/Pagos
    /// </summary>
    public class CursosPagosResponse
    {
        public List<CursoPago> Cursos { get; set; } = new();
        public decimal MontoTotal { get; set; }
    }

    public class CarritosInscripcionApiResponse
    {
        public List<CarritoSeniaApiDto> Carritos { get; set; } = new();
        public EstadoCuentaApiDto? EstadoCuenta { get; set; }
    }

    /// <summary>
    /// DTO de curso para pago.
    /// </summary>
    public class CursoPago
    {
        public long IdCurso { get; set; }
        public string? NombreCurso { get; set; }
        public decimal Monto { get; set; }
        public string? Estado { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }

    /// <summary>
    /// Response de creación de factura.
    /// Corresponde a: POST /Carritos/UltCrearFactura
    /// </summary>
    public class CrearFacturaResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long? IdFactura { get; set; }
        public string? NumeroFactura { get; set; }
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

        /// <summary>
        /// Confirma una preinscripción en la API de Inscripciones y Pagos.
        /// Corresponde a: POST /ConfirmarPreInscripcion
        /// </summary>
        /// <param name="request">Datos de la preinscripción a confirmar</param>
        /// <returns>Resultado de la confirmación</returns>
        public async Task<OperationResult<ConfirmarPreInscripcionApiResponse>> ConfirmarPreInscripcionAsync(
            ConfirmarPreInscripcionApiRequest request)
        {
            try
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

                var response = await _httpClient.PostAsJsonAsync(url, request.Turno);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ConfirmarPreInscripcionApiResponse>();
                    return OperationResult<ConfirmarPreInscripcionApiResponse>.Ok(
                        result!,
                        nameof(ConfirmarPreInscripcionAsync)
                    );
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<ConfirmarPreInscripcionApiResponse>.IsFailed(
                    "CONFIRMAR_PREINSCRIPCION_01",
                    nameof(ConfirmarPreInscripcionAsync),
                    $"La API rechazó la confirmación: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<ConfirmarPreInscripcionApiResponse>(ex, nameof(ConfirmarPreInscripcionAsync));
            }
        }

        /// <summary>
        /// Obtiene la seña mínima a pagar de una inscripción existente (read-only).
        /// Corresponde a: GET /Inscripciones/SeniaMinima
        /// </summary>
        public async Task<OperationResult<SeniaMinimaApiResponse>> ObtenerSeniaMinimaAsync(long idInscripto, long idProducto)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Obteniendo seña mínima - Inscripto: {IdInscripto}, Producto: {IdProducto}", idInscripto, idProducto);
                }

                var url = $"ORTSecure/Inscripciones/SeniaMinima?idInscripto={idInscripto}&idProducto={idProducto}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SeniaMinimaApiResponse>();
                    return OperationResult<SeniaMinimaApiResponse>.Ok(result!, nameof(ObtenerSeniaMinimaAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<SeniaMinimaApiResponse>.IsFailed(
                    "SENIA_MINIMA_01",
                    nameof(ObtenerSeniaMinimaAsync),
                    $"Error al obtener la seña mínima: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!);
            }
            catch (Exception ex)
            {
                return HandleException<SeniaMinimaApiResponse>(ex, nameof(ObtenerSeniaMinimaAsync));
            }
        }

        /// <summary>
        /// Obtiene las ofertas disponibles para inscripción en admisiones (sin proceso).
        /// Corresponde a: GET /OfertasParaInscripcionAdmisiones
        /// </summary>
        /// <param name="idProducto">ID del producto</param>
        /// <param name="idComienzo">ID del comienzo</param>
        /// <param name="idTurno">ID del turno</param>
        /// <returns>Lista de ofertas disponibles</returns>
        public async Task<OperationResult<List<OfertaInscripcionDto>>> ObtenerOfertasParaInscripcionAdmisionesAsync(
            long idProducto,
            long idComienzo,
            long idTurno)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                    "Obteniendo ofertas para inscripción - Producto: {IdProducto}, Comienzo: {IdComienzo}, Turno: {IdTurno}",
                    idProducto,
                    idComienzo,
                    idTurno
                    );
                }
                var url = $"ORTSecure/Inscripciones/OfertasParaInscripcionAdmisiones?idProducto={idProducto}&idComienzo={idComienzo}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var ofertasApi = await response.Content.ReadFromJsonAsync<List<OfertaInscripcionApiResponse>>();
                    var result = MapearOfertasInscripcion(ofertasApi);
                    return OperationResult<List<OfertaInscripcionDto>>.Ok(
                        result!,
                        nameof(ObtenerOfertasParaInscripcionAdmisionesAsync)
                    );
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<List<OfertaInscripcionDto>>.IsFailed(
                    "OFERTAS_INSCRIPCION_01",
                    nameof(ObtenerOfertasParaInscripcionAdmisionesAsync),
                    $"Error al obtener ofertas: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<List<OfertaInscripcionDto>>(
                    ex,
                    nameof(ObtenerOfertasParaInscripcionAdmisionesAsync)
                );
            }
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
            try
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
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var ofertasApi = await response.Content.ReadFromJsonAsync<List<OfertaInscripcionApiResponse>>();
                    var result = MapearOfertasInscripcion(ofertasApi);
                    return OperationResult<List<OfertaInscripcionDto>>.Ok(
                        result!,
                        nameof(ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync)
                    );
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<List<OfertaInscripcionDto>>.IsFailed(
                    "OFERTAS_INSCRIPCION_PROCESO_01",
                    nameof(ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync),
                    $"Error al obtener ofertas con proceso: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<List<OfertaInscripcionDto>>(
                    ex,
                    nameof(ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync)
                );
            }
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
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Consultando cuenta corriente con estado: {Estado}", estado);
                }
                var url = $"ORTSecure/Pagos/CtaCte?estado={Uri.EscapeDataString(estado)}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CtaCteResponse>();
                    return OperationResult<CtaCteResponse>.Ok(result!, nameof(ObtenerCtaCteAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<CtaCteResponse>.IsFailed(
                    "CTACTE_GET_01",
                    nameof(ObtenerCtaCteAsync),
                    $"Error al obtener cuenta corriente: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<CtaCteResponse>(ex, nameof(ObtenerCtaCteAsync));
            }
        }

        /// <summary>
        /// Obtiene la lista de cursos pendientes de pago.
        /// Corresponde a: GET /api/Pagos
        /// </summary>
        /// <returns>Lista de cursos con sus montos a pagar</returns>
        public async Task<OperationResult<CursosPagosResponse>> ObtenerCursosPagosAsync()
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Consultando cursos pendientes de pago");
                }
                var response = await _httpClient.GetAsync("ORTSecure/Pagos/Carritos");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CursosPagosResponse>();
                    return OperationResult<CursosPagosResponse>.Ok(result!, nameof(ObtenerCursosPagosAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<CursosPagosResponse>.IsFailed(
                    "CURSOS_PAGOS_GET_01",
                    nameof(ObtenerCursosPagosAsync),
                    $"Error al obtener cursos a pagar: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<CursosPagosResponse>(ex, nameof(ObtenerCursosPagosAsync));
            }
        }

        public async Task<OperationResult<CarritosInscripcionApiResponse>> ObtenerCarritosPorInscripcionAsync(long idInscripcion)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Consultando carritos de la inscripciÃ³n: {IdInscripcion}", idInscripcion);
                }

                var response = await _httpClient.GetAsync($"ORTSecure/Pagos/Carritos?idInscripcion={idInscripcion}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CarritosInscripcionApiResponse>();
                    return OperationResult<CarritosInscripcionApiResponse>.Ok(result!, nameof(ObtenerCarritosPorInscripcionAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<CarritosInscripcionApiResponse>.IsFailed(
                    "CARRITOS_INSCRIPCION_GET_01",
                    nameof(ObtenerCarritosPorInscripcionAsync),
                    $"Error al obtener carritos de la inscripciÃ³n: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!);
            }
            catch (Exception ex)
            {
                return HandleException<CarritosInscripcionApiResponse>(ex, nameof(ObtenerCarritosPorInscripcionAsync));
            }
        }

        /// <summary>Procesa el pago de los carritos de seña de una inscripción contra la API legacy.</summary>
        public async Task<OperationResult<List<DtoMensajePagoCarrito>>> PagarCarritosPorInscripcionAsync(
            long idInscripcion,
            string tipoPago = "PAGO_CUENTA_CORRIENTE")
        {
            try
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
                var response = await _httpClient.PostAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<DtoMensajePagoCarrito>>();
                    return OperationResult<List<DtoMensajePagoCarrito>>.Ok(result!, nameof(PagarCarritosPorInscripcionAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<List<DtoMensajePagoCarrito>>.IsFailed(
                    "PAGAR_CARRITOS_01",
                    nameof(PagarCarritosPorInscripcionAsync),
                    $"La API rechazó el pago: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<List<DtoMensajePagoCarrito>>(ex, nameof(PagarCarritosPorInscripcionAsync));
            }
        }


        public async Task<OperationResult<string>> ObtenerUrlCrearFacturaPorInscripcionAsync(
            long idInscripcion,
            string tipoPago,
            string banco = "")
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Obteniendo URL de factura de la inscripción {IdInscripcion} con tipo de pago: {TipoPago}",
                        idInscripcion,
                        tipoPago);
                }

                var url = $"ORTSecure/Pagos/Carritos/UrlCrearFactura?tipoPago={Uri.EscapeDataString(tipoPago)}&idInscripcion={idInscripcion}&banco={Uri.EscapeDataString(banco)}";
                var response = await _httpClient.PostAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<string>();
                    return OperationResult<string>.Ok(result!, nameof(ObtenerUrlCrearFacturaPorInscripcionAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<string>.IsFailed(
                    "URL_CREAR_FACTURA_01",
                    nameof(ObtenerUrlCrearFacturaPorInscripcionAsync),
                    $"La API rechazó la creación de URL de factura: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!);
            }
            catch (Exception ex)
            {
                return HandleException<string>(ex, nameof(ObtenerUrlCrearFacturaPorInscripcionAsync));
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
