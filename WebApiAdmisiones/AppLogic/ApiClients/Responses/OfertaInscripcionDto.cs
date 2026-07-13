using AppLogic.ApiClients.Dtos;

namespace AppLogic.ApiClients.Responses
{
    /// <summary>
    /// DTO para oferta de inscripción (usado en los GET de ofertas).
    /// </summary>
    public class OfertaInscripcionDto
    {
        public long IdOferta { get; set; }
        public DtoTurno Turno { get; set; } = new();
        public string? HorarioReferencia { get; set; }
    }
}
