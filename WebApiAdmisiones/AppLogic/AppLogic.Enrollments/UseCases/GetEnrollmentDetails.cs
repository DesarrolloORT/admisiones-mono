using AppLogic.Contracts;
using AppLogic.Contracts.Constants;
using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Mapping;
using AppLogic.Enrollments.Rules;
using AppLogic.Integrations.EnrollmentsAndPayments.Interfaces;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Enrollments.UseCases;

public class GetEnrollmentDetails(
    IUnitOfWorkFactory uowFactory,
    IEnrollmentsAndPaymentsApiClient apiClient) : IGetEnrollmentDetails
{
    private const string MethodName = nameof(GetEnrollmentDetails);

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IEnrollmentsAndPaymentsApiClient _apiClient = apiClient;

    public async Task<OperationResult<EnrollmentDetailsResponse>> ExecuteAsync(long personId, long productId, long admissionProcessId)
    {
        using var uow = _uowFactory.Create();

        var level1And2Rows = uow.VdInscripcionesFresco1y2s.GetInscripcionesFrescoHabilitadas(personId, productId, admissionProcessId)
            .OrderBy(x => x.FechaInicioComienzo)
            .ToList();
        var estado = level1And2Rows.Count > 0 ? level1And2Rows[0].EstadoInscripcion : null;
        var productFullName = level1And2Rows.Count > 0 ? level1And2Rows[0].NombreExtensoProducto : null;
        var filas = level1And2Rows.Select(EnrollmentPaymentRows.From).ToList();

        if (estado == null)
        {
            var level3And4Rows = uow.VdInscripcionesFresco3y4s.GetInscripcionesFrescoHabilitadas(personId, productId, admissionProcessId)
                .OrderBy(x => x.FechaInicioComienzo)
                .ToList();
            estado = level3And4Rows.Count > 0 ? level3And4Rows[0].EstadoInscripcion : null;
            productFullName = level3And4Rows.Count > 0 ? level3And4Rows[0].NombreExtensoProducto : null;
            filas = level3And4Rows.Select(EnrollmentPaymentRows.From).ToList();
        }

        if (estado == null)
        {
            return OperationResult<EnrollmentDetailsResponse>.IsFailed(
                "INS_DET_01",
                MethodName,
                "No se encontró la inscripción para la persona.",
                404);
        }

        var response = new EnrollmentDetailsResponse { Status = estado };

        switch (estado)
        {
            case EnrollmentStatus.InProgress:
                var offerings = uow.InteresProductoOfertas.GetOfertasSeleccionadas(personId, productId, admissionProcessId);
                response.InProgress = offerings.Count > 0 ? EnrollmentMapper.MapInProgressDetails(offerings) : null;
                break;

            case EnrollmentStatus.PaymentPending:
                var pendingPaymentError = await BuildPendingPaymentAsync(uow, response, personId, productId, productFullName, filas);
                if (pendingPaymentError != null)
                {
                    return pendingPaymentError;
                }
                break;

            case EnrollmentStatus.Confirmed:
                var confirmadaDetalle = ConfirmedEnrollmentDetails.Build(uow, personId, filas.Select(f => f.IdInscripto));
                if (confirmadaDetalle == null)
                {
                    return OperationResult<EnrollmentDetailsResponse>.IsFailed(
                        "INS_DET_02",
                        MethodName,
                        "No se encontró la inscripción confirmada para la persona.",
                        404);
                }
                response.Confirmed = confirmadaDetalle;
                break;

            // "A la espera" y estados desconocidos: se devuelve solo el estado, sin detalle.
        }

        return OperationResult<EnrollmentDetailsResponse>.Ok(response, MethodName);
    }

    /// <summary>
    /// Arma el detalle de una o varias inscripciones (nivel 3 y 4 con seminarios puede traer más de una
    /// oferta para el mismo producto/proceso) en estado "Pago pendiente". Si ya eligió método de pago
    /// (existe reserva mínima) devuelve el bloque compacto <see cref="MinimumDepositDetails"/>; si no, el payload
    /// completo. Devuelve un resultado de error para cortar, o null si completó el response correctamente.
    /// </summary>
    private async Task<OperationResult<EnrollmentDetailsResponse>?> BuildPendingPaymentAsync(
        IUnitOfWork uow, EnrollmentDetailsResponse response, long personId, long productId, string? productFullName, List<EnrollmentPaymentRow> filas)
    {
        if (filas.Count == 0)
        {
            return OperationResult<EnrollmentDetailsResponse>.IsFailed(
                "INS_DET_02",
                MethodName,
                "No se encontró la inscripción para la persona.",
                404);
        }

        // Solo se necesita T_INSCRIPTO para la fecha de vencimiento de pago (no está en la vista fresco).
        var cabecera = uow.Inscriptos.GetDetalleByKey(filas[0].IdInscripto, personId);
        if (cabecera == null)
        {
            return OperationResult<EnrollmentDetailsResponse>.IsFailed(
                "INS_DET_02",
                MethodName,
                "No se encontró la inscripción para la persona.",
                404);
        }

        var carritos = await _apiClient.GetCartsByEnrollmentAsync(filas.Select(f => f.IdInscripto));
        if (!carritos.Success)
            return carritos.Failure().As<EnrollmentDetailsResponse>(MethodName);

        var reservaMinima = uow.InscriptoSeniaMinima.GetByKey(filas[0].IdInscripto);
        if (reservaMinima != null)
        {
            var person = uow.Personas.GetByKey(personId);
            response.MinimumDeposit = new MinimumDepositDetails
            {
                PaymentType = reservaMinima.MetodoPagoSeniaMinima,
                DocumentNumber = person?.Documento?.Trim(),
                PersonId = personId,
                DepositAmount = PreEnrollmentConfirmationRules.AddDepositPayment(carritos.Data?.Carritos)
            };
        }
        else
        {
            response.PendingPayment = EnrollmentMapper.MapPendingPayment(filas, productId, productFullName, cabecera.FechaVtoInscr, carritos.Data);
        }
        return null;
    }
}
