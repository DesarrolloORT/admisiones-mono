
namespace AppLogic.ApiClients.Dtos
{
    /// <summary>
    /// DTO para oferta de inscripción (usado en los GET de ofertas).
    /// </summary>
    public class OfertaInscripcionDto
    {
        public long IdOferta { get; set; }
        public DtoTurno Turno { get; set; } = new();
        public string? HorarioReferencia { get; set; }
        public DateTime? FechaReferencia { get; set; }
        public string? DescripcionOferta { get; set; }
    }
}
