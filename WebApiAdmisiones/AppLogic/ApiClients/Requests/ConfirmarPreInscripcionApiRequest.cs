using AppLogic.ApiClients.Dtos;

namespace AppLogic.ApiClients.Requests
{
    /// <summary>
    /// Request para confirmar una preinscripción.
    /// Corresponde a: POST /ConfirmarPreInscripcion
    /// </summary>
    public class ConfirmarPreInscripcionApiRequest
    {
        public DtoTurno Turno { get; set; } = new();
        public string TipoInscripcion { get; set; } = string.Empty;
        public long IdProducto { get; set; }
        public long IdProceso { get; set; }
        public long IdOfertaSeleccionada { get; set; }
    }
}
