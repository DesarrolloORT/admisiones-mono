namespace AppLogic.Enrollments.Dtos;

/// <summary>
/// Mensaje de resultado de un pago, tal como lo recibe el front.
/// Es el equivalente propio de <c>CartPaymentMessage</c>: ese modela el formato de la API interna,
/// este es nuestro contrato.
/// </summary>
public class PaymentMessage
{
    /// <summary>Clave del mensaje.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Texto del mensaje que se muestra al usuario.</summary>
    public string Value { get; set; } = string.Empty;
}
