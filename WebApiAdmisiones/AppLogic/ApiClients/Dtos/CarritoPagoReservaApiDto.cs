using System.Text.Json.Serialization;

namespace AppLogic.ApiClients.Dtos
{
    public class CarritoPagoReservaApiDto
    {
        public string IdCarrito { get; set; } = string.Empty;

        // LogicaORT envía este importe con la clave "senia"; se expone como PagoReserva hacia adentro.
        [JsonPropertyName("senia")]
        public decimal PagoReserva { get; set; }
    }
}
