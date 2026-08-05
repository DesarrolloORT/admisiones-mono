using AppLogic.Contracts;
using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Validation;
using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
using AppLogic.Integrations.EnrollmentsAndPayments.Interfaces;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Enrollments.UseCases.Payments;

public class PayWithPersonalAccount(
    IUnitOfWorkFactory uowFactory,
    IEnrollmentsAndPaymentsApiClient apiClient) : IPayWithPersonalAccount
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IEnrollmentsAndPaymentsApiClient _apiClient = apiClient;

    public async Task<OperationResult<List<CartPaymentMessage>>> ExecuteAsync(long personId, PayWithPersonalAccountRequest request)
    {
        const string methodName = nameof(PayWithPersonalAccount);

        if (request == null)
            return OperationResult<List<CartPaymentMessage>>.IsFailed("INS_PC_00", methodName, "Request invalido.", 400);

        if (!RequestedEnrollments.HasValidIds(request.EnrollmentIds))
            return OperationResult<List<CartPaymentMessage>>.IsFailed("INS_PC_01", methodName, "IdsInscripcion invalido.", 400);

        using (var uow = _uowFactory.Create())
        {
            if (!RequestedEnrollments.AllBelongToPerson(uow, request.EnrollmentIds, personId))
                return OperationResult<List<CartPaymentMessage>>.IsFailed("INS_PC_02", methodName, "No se encontro la inscripcion para la persona.", 404);
        }

        var paymentResult = await _apiClient.PayCartsByEnrollmentAsync(request.EnrollmentIds);
        return paymentResult.Success
            ? OperationResult<List<CartPaymentMessage>>.Ok(paymentResult.Data, methodName)
            : paymentResult.Failure().As<List<CartPaymentMessage>>(methodName);
    }
}
