namespace AppLogic.Inscripciones.Dtos
{
    public class DtoGuardarMetodoPagoRequest
    {
        public long IdInscripcion { get; set; }
        public string TipoPago { get; set; } = string.Empty;
    }

    public class DtoObtenerUrlFacturaRequest
    {
        public long IdInscripcion { get; set; }
        public string TipoPago { get; set; } = string.Empty;
        public string? IdBancoSistarbanc { get; set; }
    }

    public class DtoPagarCuentaPersonalRequest
    {
        public long IdInscripcion { get; set; }
    }

    public class DtoPagarRequest
    {
        public long IdInscripcion { get; set; }
        public string TipoPago { get; set; } = string.Empty;
        public string? IdBancoSistarbanc { get; set; }
    }
}
