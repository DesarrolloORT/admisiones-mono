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

    public class ResumenInscripcionApiDto
    {
        public long IdOferta { get; set; }
        public long IdProducto { get; set; }
        public string? Carrera { get; set; }
        public long IdComienzo { get; set; }
        public string? Comienzo { get; set; }
        public long IdTurno { get; set; }
        public string? Turno { get; set; }
    }

    public class EstadoCuentaApiDto
    {
        public decimal SaldoActual { get; set; }
    }
}
