using System.Collections.Generic;

namespace AppLogic.Inscripciones.Dtos;

public class DtoGuardarMetodoPagoRequest
{
    public List<long> IdsInscripcion { get; set; } = new();
    public string TipoPago { get; set; } = string.Empty;
}
