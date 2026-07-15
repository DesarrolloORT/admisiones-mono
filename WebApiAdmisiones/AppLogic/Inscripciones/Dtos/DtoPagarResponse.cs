using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppLogic.Inscripciones.Dtos
{
    public class DtoPagarResponse
    {
        public string Resultado { get; set; } = string.Empty;
        public string? UrlPago { get; set; }
        public string? ParametrosEncriptados { get; set; }
        public List<DtoMensajePagoCarrito> Mensajes { get; set; } = new();

        /// <summary>Detalle de la inscripción confirmada. Solo se completa cuando el pago se completa (CUENTA_PERSONAL).</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DtoConfirmadaDetalle? Confirmada { get; set; }
    }

    public class DtoMensajePagoCarrito
    {
        public string Clave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }
}
