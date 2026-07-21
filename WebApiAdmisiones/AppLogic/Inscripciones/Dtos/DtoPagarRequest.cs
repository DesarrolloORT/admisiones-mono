namespace AppLogic.Inscripciones.Dtos
{
    public class DtoPagarRequest
    {
        public long IdInscripcion { get; set; }
        public string TipoPago { get; set; } = string.Empty;
        public string? IdBancoSistarbanc { get; set; }
    }
}
