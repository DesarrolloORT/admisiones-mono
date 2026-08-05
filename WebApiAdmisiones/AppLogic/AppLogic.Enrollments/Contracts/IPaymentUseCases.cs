using AppLogic.Enrollments.Dtos;
using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
using Utilities;

namespace AppLogic.Enrollments.Contracts;

/// <summary>Inicia o confirma el pago de una inscripción según el tipo de pago elegido.</summary>
public interface IStartEnrollmentPayment
{
    Task<OperationResult<StartPaymentResponse>> ExecuteAsync(long personId, StartPaymentRequest request);
}

/// <summary>Paga los carritos de la inscripción contra la cuenta personal del alumno.</summary>
public interface IPayWithPersonalAccount
{
    Task<OperationResult<List<CartPaymentMessage>>> ExecuteAsync(long personId, PayWithPersonalAccountRequest request);
}

/// <summary>Registra la reserva mínima con un método de pago externo (Abitab, Paganza).</summary>
public interface IRegisterExternalPaymentMethod
{
    OperationResult<bool> Execute(long personId, RegisterExternalPaymentMethodRequest request);
}

/// <summary>Genera la URL de factura para pagar por Banred, Geopay o Sistarbanc.</summary>
public interface IGenerateInvoicePaymentUrl
{
    Task<OperationResult<InvoicePaymentUrlResponse>> ExecuteAsync(long personId, GenerateInvoicePaymentUrlRequest request);
}
