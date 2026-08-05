using AppLogic.Contracts;
using AppLogic.Enrollments.Constants;
using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Rules;
using AppLogic.Enrollments.Validation;
using AppLogic.Integrations.EnrollmentsAndPayments.Interfaces;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Enrollments.UseCases.Payments;

public class GenerateInvoicePaymentUrl(
    IUnitOfWorkFactory uowFactory,
    IEnrollmentsAndPaymentsApiClient apiClient) : IGenerateInvoicePaymentUrl
{
    private static readonly HashSet<string> InvoicePaymentTypes =
        [EnrollmentConstants.PaymentType.Banred, EnrollmentConstants.PaymentType.Sistarbanc, EnrollmentConstants.PaymentType.Geopay];

    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IEnrollmentsAndPaymentsApiClient _apiClient = apiClient;

    public async Task<OperationResult<InvoicePaymentUrlResponse>> ExecuteAsync(long personId, GenerateInvoicePaymentUrlRequest request)
    {
        const string methodName = nameof(GenerateInvoicePaymentUrl);

        if (request == null)
            return OperationResult<InvoicePaymentUrlResponse>.IsFailed("INS_UF_00", methodName, "Request invalido.", 400);

        if (!RequestedEnrollments.HasValidIds(request.EnrollmentIds))
            return OperationResult<InvoicePaymentUrlResponse>.IsFailed("INS_UF_01", methodName, "IdsInscripcion invalido.", 400);

        var normalizedPaymentType = request.PaymentType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!InvoicePaymentTypes.Contains(normalizedPaymentType))
        {
            return OperationResult<InvoicePaymentUrlResponse>.IsFailed("INS_UF_02", methodName, "TipoPago invalido.", 400);
        }

        var sistarbancBankId = request.SistarbancBankId?.Trim();
        if (normalizedPaymentType == EnrollmentConstants.PaymentType.Sistarbanc && string.IsNullOrWhiteSpace(sistarbancBankId))
        {
            return OperationResult<InvoicePaymentUrlResponse>.IsFailed("INS_UF_03", methodName, "IdBancoSistarbanc requerido para SISTARBANC.", 400);
        }

        using (var uow = _uowFactory.Create())
        {
            if (!RequestedEnrollments.AllBelongToPerson(uow, request.EnrollmentIds, personId))
                return OperationResult<InvoicePaymentUrlResponse>.IsFailed("INS_UF_04", methodName, "No se encontro la inscripcion para la persona.", 404);
        }

        var bank = normalizedPaymentType == EnrollmentConstants.PaymentType.Sistarbanc ? sistarbancBankId! : string.Empty;
        var urlResult = await _apiClient.GetCreateInvoiceUrlByEnrollmentAsync(request.EnrollmentIds, normalizedPaymentType, bank);
        if (!urlResult.Success)
            return urlResult.Failure().As<InvoicePaymentUrlResponse>(methodName);

        var (url, parametrosEncriptados) = InvoicePaymentUrl.Split(urlResult.Data);
        return OperationResult<InvoicePaymentUrlResponse>.Ok(
            new InvoicePaymentUrlResponse { Url = url, EncryptedParameters = parametrosEncriptados },
            methodName);
    }
}
