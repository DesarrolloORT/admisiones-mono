using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.Personas.Dtos;

[ExcludeFromCodeCoverage]
public class DtoInscripcionesPorProductoProcesoResponse
{
    public decimal? IdProducto { get; set; }
    public string? NombreExtensoProducto { get; set; }
    public decimal? IdProceso { get; set; }
    public long? IdNivelProducto { get; set; }
    public string? EstadoInscripcion { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProgConSeminariosProducto { get; set; }
    public List<DtoInscripcionItemResponse> Inscripciones { get; set; } = [];
}

[ExcludeFromCodeCoverage]
public class DtoInscripcionItemResponse
{
    public decimal? IdInscripto { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? IdOferta { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DescripcionOferta { get; set; }
    public decimal? IdTurno { get; set; }
    public decimal? IdComienzo { get; set; }
    public DateTime? FechaInicioComienzo { get; set; }
    public string? NombreComienzo { get; set; }
    public string? NombreTurno { get; set; }
    public DateTime? FechaReferencia { get; set; }
}
