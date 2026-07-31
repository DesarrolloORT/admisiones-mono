using System.Collections.Generic;

namespace AppLogic.Inscripciones.Dtos;

public class DtoPagarRequest
{
    /// <summary>Inscripciones a pagar (una para nivel 1 y 2; una o varias para nivel 3 y 4 con seminarios).</summary>
    public List<long> IdsInscripcion { get; set; } = new();
    public string TipoPago { get; set; } = string.Empty;
    public string? IdBancoSistarbanc { get; set; }
}
