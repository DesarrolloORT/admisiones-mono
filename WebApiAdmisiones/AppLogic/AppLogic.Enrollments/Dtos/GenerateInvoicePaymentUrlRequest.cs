namespace AppLogic.Enrollments.Dtos;

/// <summary>Generación de la URL de factura para pagar en un sitio externo.</summary>
public class GenerateInvoicePaymentUrlRequest
{
    /// <summary>Inscripciones a facturar.</summary>
    public List<long> EnrollmentIds { get; set; } = new();

    /// <summary>Método de pago: BANRED, GEOPAY o SISTARBANC.</summary>
    public string PaymentType { get; set; } = string.Empty;

    /// <summary>Banco elegido, obligatorio solo para SISTARBANC.</summary>
    public string? SistarbancBankId { get; set; }
}
