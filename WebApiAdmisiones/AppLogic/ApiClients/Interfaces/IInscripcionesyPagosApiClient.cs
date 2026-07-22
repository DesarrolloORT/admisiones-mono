using System.Collections.Generic;
using System.Threading.Tasks;
using AppLogic.ApiClients.Dtos;
using AppLogic.Inscripciones.Dtos;
using Utilities;

namespace AppLogic.ApiClients.Interfaces;

/// <summary>
/// Cliente tipado para la API de Inscripciones y Pagos.
/// El ServiceAuthenticationHandler inyecta automáticamente los tokens en TODOS los métodos.
/// </summary>
public interface IInscripcionesyPagosApiClient
{
    /// <summary>
    /// Confirma una preinscripción (una o varias ofertas a la vez) en la API de Inscripciones y Pagos.
    /// Corresponde a: POST /ConfirmarPreInscripcionMultiple
    /// </summary>
    Task<OperationResult<ConfirmarPreInscripcionMultipleApiResponse>> ConfirmarPreInscripcionMultipleAsync(
        ConfirmarPreInscripcionMultipleApiRequest request);

    /// <summary>
    /// Obtiene la seña mínima a pagar de una inscripción existente (read-only).
    /// Corresponde a: GET /Inscripciones/SeniaMinima
    /// </summary>
    Task<OperationResult<SeniaMinimaApiResponse>> ObtenerSeniaMinimaAsync(long idInscripto, long idProducto);

    /// <summary>
    /// Obtiene las ofertas disponibles para inscripción en admisiones con proceso.
    /// Corresponde a: GET /OfertasParaInscripcionAdmisionesConProceso
    /// </summary>
    Task<OperationResult<List<OfertaInscripcionDto>>> ObtenerOfertasParaInscripcionAdmisionesConProcesoAsync(
        long idProducto,
        long idProceso);

    /// <summary>
    /// Obtiene el estado de cuenta corriente.
    /// Corresponde a: GET /api/Pagos/CtaCte
    /// </summary>
    Task<OperationResult<CtaCteResponse>> ObtenerCtaCteAsync(string estado = "SALDO_ACTUAL_Y_MOVIMIENTOS");

    /// <summary>
    /// Obtiene la lista de cursos pendientes de pago.
    /// Corresponde a: GET /api/Pagos
    /// </summary>
    Task<OperationResult<CursosPagosResponse>> ObtenerCursosPagosAsync();

    Task<OperationResult<CarritosInscripcionApiResponse>> ObtenerCarritosPorInscripcionAsync(long idInscripcion);

    /// <summary>Procesa el pago de los carritos de seña de una inscripción contra la API legacy.</summary>
    Task<OperationResult<List<DtoMensajePagoCarrito>>> PagarCarritosPorInscripcionAsync(
        long idInscripcion,
        string tipoPago = "PAGO_CUENTA_CORRIENTE");

    Task<OperationResult<string>> ObtenerUrlCrearFacturaPorInscripcionAsync(
        long idInscripcion,
        string tipoPago,
        string banco = "");
}
