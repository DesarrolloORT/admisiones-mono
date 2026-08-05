using AppLogic.Enrollments.Mapping;
using AppLogic.Contracts;
using AppLogic.Enrollments.Constants;
using AppLogic.Enrollments.Contracts;
using AppLogic.Enrollments.Dtos;
using AppLogic.Enrollments.Rules;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Enrollments.UseCases.Payments;

/// <summary>
/// Único punto de entrada del pago: según el tipo elegido despacha al caso de uso que corresponde.
/// No contiene lógica de pago propia, solo el ruteo y el armado del response común.
/// </summary>
public class StartEnrollmentPayment(
    IUnitOfWorkFactory uowFactory,
    IPayWithPersonalAccount payWithPersonalAccount,
    IRegisterExternalPaymentMethod registerExternalPaymentMethod,
    IGenerateInvoicePaymentUrl generateInvoicePaymentUrl) : IStartEnrollmentPayment
{
    private readonly IUnitOfWorkFactory _uowFactory = uowFactory;
    private readonly IPayWithPersonalAccount _payWithPersonalAccount = payWithPersonalAccount;
    private readonly IRegisterExternalPaymentMethod _registerExternalPaymentMethod = registerExternalPaymentMethod;
    private readonly IGenerateInvoicePaymentUrl _generateInvoicePaymentUrl = generateInvoicePaymentUrl;

    public async Task<OperationResult<StartPaymentResponse>> ExecuteAsync(long personId, StartPaymentRequest request)
    {
        const string methodName = nameof(StartEnrollmentPayment);

        if (request == null)
        {
            return OperationResult<StartPaymentResponse>.IsFailed("INS_PAG_00", methodName, "Request invalido.", 400);
        }

        var paymentType = request.PaymentType?.Trim().ToUpperInvariant();
        return paymentType switch
        {
            EnrollmentConstants.PaymentType.CuentaPersonal =>
                await PayWithPersonalAccountAsync(personId, request, methodName),

            EnrollmentConstants.PaymentType.Abitab or EnrollmentConstants.PaymentType.Paganza =>
                SaveExternalMethod(personId, request, paymentType, methodName),

            EnrollmentConstants.PaymentType.Banred or EnrollmentConstants.PaymentType.Geopay or EnrollmentConstants.PaymentType.Sistarbanc =>
                await GenerateInvoiceUrlAsync(personId, request, paymentType, methodName),

            _ => OperationResult<StartPaymentResponse>.IsFailed("INS_PAG_01", methodName, "TipoPago invalido.", 400)
        };
    }

    private async Task<OperationResult<StartPaymentResponse>> PayWithPersonalAccountAsync(
        long personId, StartPaymentRequest request, string methodName)
    {
        var result = await _payWithPersonalAccount.ExecuteAsync(
            personId,
            new PayWithPersonalAccountRequest { EnrollmentIds = request.EnrollmentIds });
        if (!result.Success)
            return result.Failure().As<StartPaymentResponse>(methodName);

        using var uow = _uowFactory.Create();
        var detalle = ConfirmedEnrollmentDetails.Build(uow, personId, request.EnrollmentIds);
        return OperationResult<StartPaymentResponse>.Ok(
            new StartPaymentResponse
            {
                Result = EnrollmentConstants.PaymentResult.PagoConfirmado,
                Messages = EnrollmentMapper.ToPaymentMessages(result.Data),
                Confirmed = detalle
            },
            methodName);
    }

    private OperationResult<StartPaymentResponse> SaveExternalMethod(
        long personId, StartPaymentRequest request, string? paymentType, string methodName)
    {
        var result = _registerExternalPaymentMethod.Execute(
            personId,
            new RegisterExternalPaymentMethodRequest { EnrollmentIds = request.EnrollmentIds, PaymentType = paymentType });

        return result.Success
            ? OperationResult<StartPaymentResponse>.Ok(
                new StartPaymentResponse { Result = EnrollmentConstants.PaymentResult.MetodoGuardado }, methodName)
            : result.Failure().As<StartPaymentResponse>(methodName);
    }

    private async Task<OperationResult<StartPaymentResponse>> GenerateInvoiceUrlAsync(
        long personId, StartPaymentRequest request, string? paymentType, string methodName)
    {
        var result = await _generateInvoicePaymentUrl.ExecuteAsync(personId, new GenerateInvoicePaymentUrlRequest
        {
            EnrollmentIds = request.EnrollmentIds,
            PaymentType = paymentType,
            SistarbancBankId = request.SistarbancBankId
        });

        return result.Success
            ? OperationResult<StartPaymentResponse>.Ok(new StartPaymentResponse
            {
                Result = EnrollmentConstants.PaymentResult.UrlGenerada,
                PaymentUrl = result.Data!.Url,
                EncryptedParameters = result.Data.EncryptedParameters
            }, methodName)
            : result.Failure().As<StartPaymentResponse>(methodName);
    }
}
