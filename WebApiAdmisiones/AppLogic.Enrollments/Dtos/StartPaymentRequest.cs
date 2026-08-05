namespace AppLogic.Enrollments.Dtos;

/// <summary>Inicio del pago de una o varias inscripciones.</summary>
public class StartPaymentRequest
{
    /// <summary>Inscripciones a pagar (una para nivel 1 y 2; una o varias para nivel 3 y 4 con seminarios).</summary>
    public List<long> EnrollmentIds { get; set; } = new();

    /// <summary>Método de pago: CUENTA_PERSONAL, ABITAB, PAGANZA, BANRED, GEOPAY o SISTARBANC.</summary>
    public string PaymentType { get; set; } = string.Empty;

    /// <summary>Banco elegido, obligatorio solo para SISTARBANC.</summary>
    public string? SistarbancBankId { get; set; }
}
