using System.Text.Json.Serialization;

namespace AppLogic.Integrations.EnrollmentsAndPayments.Dtos;

/// <summary>
/// Mensaje que devuelve la API interna de Inscripciones y Pagos al pagar un carrito.
///
/// <para>
/// Los <c>[JsonPropertyName]</c> de esta clase NO son una preferencia de nomenclatura: describen el
/// formato que emite un sistema ajeno. Si se quitan, la deserialización deja de mapear.
/// Este tipo no sale al front: <c>PaymentMessage</c> es el que viaja en la respuesta.
/// </para>
/// </summary>
public class CartPaymentMessage
{
    /// <summary>Clave del mensaje que emite la API interna.</summary>
    [JsonPropertyName("clave")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Texto del mensaje que emite la API interna.</summary>
    [JsonPropertyName("valor")]
    public string Value { get; set; } = string.Empty;
}
