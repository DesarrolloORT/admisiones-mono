
using System.Text.Json.Serialization;

namespace AppLogic.ApiClients.Dtos;

/// <summary>
/// DTO para oferta de inscripción (usado en los GET de ofertas).
/// </summary>
public class OfertaInscripcionDto
{
    public long IdOferta { get; set; }
    public DtoTurno Turno { get; set; } = new();
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HorarioReferencia { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? FechaReferencia { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DescripcionOferta { get; set; }
}
