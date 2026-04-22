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

    /// <summary>
    /// DTO para turno (usado en confirmar preinscripción).
    /// </summary>
    public class DTOTurno
    {
        public long IdTurno { get; set; }
        public string? NombreTurno { get; set; }
    }

    /// <summary>
    /// Request para confirmar una preinscripción.
    /// Corresponde a: POST /ConfirmarPreInscripcion
    /// </summary>
    public class ConfirmarPreInscripcionRequest
    {
        public DTOTurno Turno { get; set; } = new();
        public string TipoInscripcion { get; set; } = string.Empty;
        public long IdProducto { get; set; }
        public long IdProceso { get; set; }
        public long IdOfertaSeleccionada { get; set; }
    }

    /// <summary>
    /// Response de confirmar preinscripción.
    /// </summary>
    public class ConfirmarPreInscripcionResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long? IdInscripcion { get; set; }
    }

    /// <summary>
    /// DTO para oferta de inscripción (usado en los GET de ofertas).
    /// </summary>
    public class OfertaInscripcionDTO
    {
        public long IdOferta { get; set; }
        public long IdProducto { get; set; }
        public long IdTurno { get; set; }
        public string? NombreTurno { get; set; }
        public long? IdComienzo { get; set; }
        public string? NombreComienzo { get; set; }
        public long? IdProceso { get; set; }
        public string? NombreProceso { get; set; }
        public int? CuposDisponibles { get; set; }
        public bool Disponible { get; set; }
    }

    /// <summary>
    /// Response para lista de ofertas.
    /// Corresponde a: GET /OfertasParaInscripcionAdmisiones y GET /OfertasParaInscripcionAdmisionesConProceso
    /// </summary>
    public class OfertasInscripcionResponse
    {
        public List<OfertaInscripcionDTO> Ofertas { get; set; } = new();
        public int TotalCount { get; set; }
    }

    #endregion

    #region DTOs - Pagos

    /// <summary>
    /// DTO para par clave-valor usado en carritos de pago.
    /// Usado en: POST /Pagos/Carritos/{id}/Pagar y POST /Carritos/UltCrearFactura
    /// </summary>
    public class ClaveValorCarrito
    {
        public string Clave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
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
    /// Response de pago realizado.
    /// Corresponde a: POST /Pagos/Carritos/{id}/Pagar
    /// </summary>
    public class PagoCarritoResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long? IdTransaccion { get; set; }
        public string? NumeroComprobante { get; set; }
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

    /// <summary>
    /// DTO de banco.
    /// Corresponde a: GET /api/Pagos/Bancos
    /// </summary>
    public class BancoDTO
    {
        public long IdBanco { get; set; }
        public string? NombreBanco { get; set; }
        public string? Codigo { get; set; }
        public bool Activo { get; set; }
    }

    /// <summary>
    /// Response de lista de bancos.
    /// </summary>
    public class BancosResponse
    {
        public List<BancoDTO> Bancos { get; set; } = new();
        public int TotalCount { get; set; }
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
        public async Task<OperationResult<ConfirmarPreInscripcionResponse>> ConfirmarPreInscripcionAsync(
            ConfirmarPreInscripcionRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Confirmando preinscripción - Producto: {IdProducto}, Proceso: {IdProceso}, Oferta: {IdOferta}",
                    request.IdProducto,
                    request.IdProceso,
                    request.IdOfertaSeleccionada
                );

                var response = await _httpClient.PostAsJsonAsync("/ConfirmarPreInscripcion", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ConfirmarPreInscripcionResponse>();
                    return OperationResult<ConfirmarPreInscripcionResponse>.Ok(
                        result!,
                        nameof(ConfirmarPreInscripcionAsync)
                    );
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<ConfirmarPreInscripcionResponse>.IsFailed(
                    "CONFIRMAR_PREINSCRIPCION_01",
                    nameof(ConfirmarPreInscripcionAsync),
                    $"La API rechazó la confirmación: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<ConfirmarPreInscripcionResponse>(ex, nameof(ConfirmarPreInscripcionAsync));
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
        public async Task<OperationResult<OfertasInscripcionResponse>> ObtenerOfertasParaInscripcionAdmisionesAsync(
            long idProducto,
            long idComienzo,
            long idTurno)
        {
            try
            {
                _logger.LogInformation(
                    "Obteniendo ofertas para inscripción - Producto: {IdProducto}, Comienzo: {IdComienzo}, Turno: {IdTurno}",
                    idProducto,
                    idComienzo,
                    idTurno
                );

                var url = $"/OfertasParaInscripcionAdmisiones?idProducto={idProducto}&idComienzo={idComienzo}&idTurno={idTurno}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OfertasInscripcionResponse>();
                    return OperationResult<OfertasInscripcionResponse>.Ok(
                        result!,
                        nameof(ObtenerOfertasParaInscripcionAdmisionesAsync)
                    );
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<OfertasInscripcionResponse>.IsFailed(
                    "OFERTAS_INSCRIPCION_01",
                    nameof(ObtenerOfertasParaInscripcionAdmisionesAsync),
                    $"Error al obtener ofertas: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<OfertasInscripcionResponse>(
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
        /// <param name="idTurno">ID del turno</param>
        /// <returns>Lista de ofertas disponibles</returns>
        public async Task<OperationResult<OfertasInscripcionResponse>> ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync(
            long idProducto,
            long idProceso,
            long idTurno)
        {
            try
            {
                _logger.LogInformation(
                    "Obteniendo ofertas para inscripción con proceso - Producto: {IdProducto}, Proceso: {IdProceso}, Turno: {IdTurno}",
                    idProducto,
                    idProceso,
                    idTurno
                );

                var url = $"/OfertasParaInscripcionAdmisionesConProceso?idProducto={idProducto}&idProceso={idProceso}&idTurno={idTurno}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OfertasInscripcionResponse>();
                    return OperationResult<OfertasInscripcionResponse>.Ok(
                        result!,
                        nameof(ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync)
                    );
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<OfertasInscripcionResponse>.IsFailed(
                    "OFERTAS_INSCRIPCION_PROCESO_01",
                    nameof(ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync),
                    $"Error al obtener ofertas con proceso: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<OfertasInscripcionResponse>(
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
                _logger.LogInformation("Consultando cuenta corriente con estado: {Estado}", estado);

                var url = $"/api/Pagos/CtaCte?estado={Uri.EscapeDataString(estado)}";
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
                _logger.LogInformation("Consultando cursos pendientes de pago");

                var response = await _httpClient.GetAsync("/api/Pagos");

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

        /// <summary>
        /// Procesa un pago de carrito.
        /// Corresponde a: POST /OMTSecure/Pagos/Carritos/{id}/Pagar
        /// </summary>
        /// <param name="idCarrito">ID del carrito a pagar</param>
        /// <param name="carritos">Lista de pares clave-valor con datos del carrito</param>
        /// <param name="tipoPago">Tipo de pago (ej: "PAGO_CUENTA_CORRIENTE_...")</param>
        /// <returns>Resultado del pago procesado</returns>
        public async Task<OperationResult<PagoCarritoResponse>> PagarCarritoAsync(
            long idCarrito,
            List<ClaveValorCarrito> carritos,
            string tipoPago = "PAGO_CUENTA_CORRIENTE")
        {
            try
            {
                _logger.LogInformation(
                    "Procesando pago de carrito {IdCarrito} con tipo de pago: {TipoPago}",
                    idCarrito,
                    tipoPago
                );

                var url = $"/OMTSecure/Pagos/Carritos/{idCarrito}/Pagar?tipoPago={Uri.EscapeDataString(tipoPago)}";
                var response = await _httpClient.PostAsJsonAsync(url, carritos);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PagoCarritoResponse>();
                    return OperationResult<PagoCarritoResponse>.Ok(result!, nameof(PagarCarritoAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<PagoCarritoResponse>.IsFailed(
                    "PAGAR_CARRITO_01",
                    nameof(PagarCarritoAsync),
                    $"La API rechazó el pago: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<PagoCarritoResponse>(ex, nameof(PagarCarritoAsync));
            }
        }

        /// <summary>
        /// Crea una factura para los carritos especificados.
        /// Corresponde a: POST /Carritos/UltCrearFactura
        /// </summary>
        /// <param name="carritos">Lista de pares clave-valor con datos de los carritos</param>
        /// <param name="tipoPago">Tipo de pago (ej: "EANRED_...")</param>
        /// <returns>Resultado de la creación de factura</returns>
        public async Task<OperationResult<CrearFacturaResponse>> CrearFacturaAsync(
            List<ClaveValorCarrito> carritos,
            string tipoPago = "EANRED")
        {
            try
            {
                _logger.LogInformation(
                    "Creando factura con {CantidadCarritos} carritos y tipo de pago: {TipoPago}",
                    carritos.Count,
                    tipoPago
                );

                var url = $"/Carritos/UltCrearFactura?tipoPago={Uri.EscapeDataString(tipoPago)}";
                var response = await _httpClient.PostAsJsonAsync(url, carritos);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CrearFacturaResponse>();
                    return OperationResult<CrearFacturaResponse>.Ok(result!, nameof(CrearFacturaAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<CrearFacturaResponse>.IsFailed(
                    "CREAR_FACTURA_01",
                    nameof(CrearFacturaAsync),
                    $"La API rechazó la creación de factura: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<CrearFacturaResponse>(ex, nameof(CrearFacturaAsync));
            }
        }

        /// <summary>
        /// Obtiene la lista de bancos disponibles.
        /// Corresponde a: GET /api/Pagos/Bancos
        /// </summary>
        /// <returns>Lista de bancos activos</returns>
        public async Task<OperationResult<BancosResponse>> ObtenerBancosAsync()
        {
            try
            {
                _logger.LogInformation("Consultando lista de bancos disponibles");

                var response = await _httpClient.GetAsync("/api/Pagos/Bancos");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<BancosResponse>();
                    return OperationResult<BancosResponse>.Ok(result!, nameof(ObtenerBancosAsync));
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult<BancosResponse>.IsFailed(
                    "BANCOS_GET_01",
                    nameof(ObtenerBancosAsync),
                    $"Error al obtener bancos: {response.StatusCode} - {errorContent}",
                    (int)response.StatusCode,
                    default!
                );
            }
            catch (Exception ex)
            {
                return HandleException<BancosResponse>(ex, nameof(ObtenerBancosAsync));
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
