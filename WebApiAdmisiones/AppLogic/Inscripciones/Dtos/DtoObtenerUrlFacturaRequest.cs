namespace AppLogic.Inscripciones.Dtos;

public class DtoObtenerUrlFacturaRequest
{
    public long IdInscripcion { get; set; }
    public string TipoPago { get; set; } = string.Empty;
    public string? IdBancoSistarbanc { get; set; }
}
