namespace AppLogic.Enrollments.Dtos;

/// <summary>URL del sitio externo donde continuar el pago.</summary>
public class InvoicePaymentUrlResponse
{
    /// <summary>URL a la que redirigir al usuario.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Parámetros que el sitio externo espera encriptados.</summary>
    public string? EncryptedParameters { get; set; }
}
