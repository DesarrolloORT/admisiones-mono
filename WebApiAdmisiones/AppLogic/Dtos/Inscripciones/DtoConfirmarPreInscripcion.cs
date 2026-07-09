using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppLogic.Dtos.Inscripciones
{
    public class DtoGuardarMetodoPagoRequest
    {
        public long IdInscripto { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
    }

    public class DtoObtenerUrlFacturaRequest
    {
        public long IdInscripto { get; set; }
        public string TipoPago { get; set; } = string.Empty;
        public string? IdBancoSistarbanc { get; set; }
    }

    public class DtoObtenerUrlFacturaResponse
    {
        public string Url { get; set; } = string.Empty;
        public string? ParametrosEncriptados { get; set; }
    }

    public class DtoPagarCuentaPersonalRequest
    {
        public long IdInscripto { get; set; }
    }

    public class DtoPagarRequest
    {
        public long IdInscripto { get; set; }
        public string TipoPago { get; set; } = string.Empty;
        public string? IdBancoSistarbanc { get; set; }
    }

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

    public class DtoConfirmarPreInscripcionRequest
    {
        public bool AceptoReglamento { get; set; }
        public long IdOfertaSeleccionada { get; set; }
    }

    public class DtoConfirmarPreInscripcionResponse
    {
        public bool Confirmada { get; set; }

        /// <summary>True cuando la preinscripción quedó pero la inscripción fue a bandeja ("A la espera"). El front muestra "Inscripción en proceso".</summary>
        public bool EnEspera { get; set; }

        public long? IdInscripcion { get; set; }
        public DateTime? FechaVencimientoPago { get; set; }
        public decimal Senia { get; set; }
        public DtoResumenInscripcion Resumen { get; set; } = new();
        public DtoEstadoCuenta? EstadoCuenta { get; set; }
    }

    public class DtoResumenInscripcion
    {
        public long IdOferta { get; set; }
        public long IdProducto { get; set; }
        public string? Carrera { get; set; }
        public long IdComienzo { get; set; }
        public string? Comienzo { get; set; }
        public long IdTurno { get; set; }
        public string? Turno { get; set; }
    }

    public class DtoEstadoCuenta
    {
        public decimal SaldoActual { get; set; }
    }
}
