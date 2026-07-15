namespace AppLogic.Inscripciones.Dtos
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
}
