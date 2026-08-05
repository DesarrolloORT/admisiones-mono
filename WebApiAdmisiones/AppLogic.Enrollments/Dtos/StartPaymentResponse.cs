using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppLogic.Enrollments.Dtos;

/// <summary>Resultado de iniciar o confirmar el pago de una inscripción.</summary>
public class StartPaymentResponse
{
    /// <summary>Qué ocurrió: pago confirmado, método de pago guardado o URL de pago generada.</summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>URL a la que redirigir para pagar en el sitio externo (Banred, Geopay, Sistarbanc).</summary>
    public string? PaymentUrl { get; set; }

    /// <summary>Parámetros que el sitio externo espera encriptados junto con la URL.</summary>
    public string? EncryptedParameters { get; set; }

    /// <summary>Mensajes devueltos al pagar con cuenta personal.</summary>
    public List<PaymentMessage> Messages { get; set; } = new();

    /// <summary>Detalle de la inscripción confirmada. Solo se completa cuando el pago se completa (CUENTA_PERSONAL).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConfirmedEnrollmentDetailsResponse? Confirmed { get; set; }
}
