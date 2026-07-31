using System.Collections.Generic;

namespace AppLogic.Inscripciones.Dtos;

public class DtoPagarCuentaPersonalRequest
{
    public List<long> IdsInscripcion { get; set; } = new();
}
