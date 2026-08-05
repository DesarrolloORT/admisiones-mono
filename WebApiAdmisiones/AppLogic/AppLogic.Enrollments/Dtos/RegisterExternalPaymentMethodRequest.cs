namespace AppLogic.Enrollments.Dtos;

/// <summary>Registro de la reserva mínima con un método de pago externo.</summary>
public class RegisterExternalPaymentMethodRequest
{
    /// <summary>Inscripciones sobre las que se registra la reserva.</summary>
    public List<long> EnrollmentIds { get; set; } = new();

    /// <summary>Método de pago externo: ABITAB o PAGANZA.</summary>
    public string PaymentType { get; set; } = string.Empty;
}
