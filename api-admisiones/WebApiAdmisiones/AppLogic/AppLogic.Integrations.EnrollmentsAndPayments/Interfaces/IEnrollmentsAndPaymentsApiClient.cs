using System.Collections.Generic;
using System.Threading.Tasks;
using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
using Utilities;

namespace AppLogic.Integrations.EnrollmentsAndPayments.Interfaces;

/// <summary>
/// Cliente tipado para la API de Inscripciones y Pagos.
/// El ServiceAuthenticationHandler inyecta automáticamente los tokens en TODOS los métodos.
/// </summary>
public interface IEnrollmentsAndPaymentsApiClient
{
    /// <summary>
    /// Confirma una preinscripción (una o varias ofertas a la vez) en la API de Inscripciones y Pagos.
    /// Corresponde a: POST /ConfirmarPreInscripcionMultiple
    /// </summary>
    Task<OperationResult<ConfirmarPreInscripcionMultipleApiResponse>> ConfirmMultiplePreEnrollmentAsync(
        ConfirmarPreInscripcionMultipleApiRequest request);

    /// <summary>
    /// Obtiene la seña mínima a pagar de una inscripción existente (read-only).
    /// Corresponde a: GET /Inscripciones/SeniaMinima
    /// </summary>
    Task<OperationResult<SeniaMinimaApiResponse>> GetMinimumDepositAsync(long idInscripto, long productId);

    /// <summary>
    /// Obtiene el estado de cuenta corriente.
    /// Corresponde a: GET /api/Pagos/CtaCte
    /// </summary>
    Task<OperationResult<CtaCteResponse>> GetCurrentAccountAsync(string estado = "SALDO_ACTUAL_Y_MOVIMIENTOS");

    /// <summary>
    /// Obtiene la lista de cursos pendientes de pago.
    /// Corresponde a: GET /api/Pagos
    /// </summary>
    Task<OperationResult<CursosPagosResponse>> GetCoursePaymentsAsync();

    /// <summary>Obtiene los carritos de seña de una o varias inscripciones (nivel 3 y 4 con seminarios puede traer más de una) en una sola llamada.</summary>
    Task<OperationResult<CarritosInscripcionApiResponse>> GetCartsByEnrollmentAsync(IEnumerable<long> idsInscripcion);

    /// <summary>Procesa el pago de los carritos de seña de una o varias inscripciones (nivel 3 y 4 con seminarios puede traer más de una) contra la API legacy.</summary>
    Task<OperationResult<List<CartPaymentMessage>>> PayCartsByEnrollmentAsync(
        IEnumerable<long> idsInscripcion,
        string paymentType = "PAGO_CUENTA_CORRIENTE");

    Task<OperationResult<string>> GetCreateInvoiceUrlByEnrollmentAsync(
        IEnumerable<long> idsInscripcion,
        string paymentType,
        string bank = "");
}
