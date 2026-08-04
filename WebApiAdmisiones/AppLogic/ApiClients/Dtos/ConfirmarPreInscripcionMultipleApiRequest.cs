using System.Collections.Generic;

namespace AppLogic.ApiClients.Dtos;

/// <summary>
/// Request para confirmar una preinscripción con varias ofertas a la vez (nivel 3 y 4).
/// Corresponde a: POST /ConfirmarPreInscripcionMultiple
/// </summary>
public class ConfirmarPreInscripcionMultipleApiRequest
{
    public DtoTurno Turno { get; set; } = new();
    public string TipoInscripcion { get; set; } = string.Empty;
    public long IdProducto { get; set; }
    public long IdProceso { get; set; }
    public List<long> IdsOfertasSeleccionadas { get; set; } = new();
}
