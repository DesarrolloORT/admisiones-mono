using System.Collections.Generic;

namespace AppLogic.Inscripciones.Dtos;

public class DtoObtenerUrlFacturaRequest
{
    public List<long> IdsInscripcion { get; set; } = new();
    public string TipoPago { get; set; } = string.Empty;
    public string? IdBancoSistarbanc { get; set; }
}
