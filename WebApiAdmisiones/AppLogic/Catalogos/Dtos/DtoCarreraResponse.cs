using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.Catalogos.Dtos;

[ExcludeFromCodeCoverage]
public class DtoCarrerasPorNivelResponse
{
    public long IdNivelProducto { get; set; }
    public string? NombreNivelProducto { get; set; }
    public List<DtoCarrerasPorEscuelaResponse> Escuelas { get; set; } = [];
}

[ExcludeFromCodeCoverage]
public class DtoCarrerasPorEscuelaResponse
{
    public long IdEscuela { get; set; }
    public string? NombreEscuela { get; set; }

    /// <summary>Nivel 1/2 (Carrera universitaria/Tecnicatura): productos directos, sin concepto de seminario.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DtoCarreraResponse>? Productos { get; set; }

    /// <summary>Nivel 3/4 (Actualizacion profesional): productos agrupados por si tienen seminario o no.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DtoCarrerasPorSeminarioResponse>? Seminarios { get; set; }
}

[ExcludeFromCodeCoverage]
public class DtoCarrerasPorSeminarioResponse
{
    public bool TieneSeminario { get; set; }
    public List<DtoCarreraResponse> Productos { get; set; } = [];
}

[ExcludeFromCodeCoverage]
public class DtoCarreraResponse
{
    public long IdProducto { get; set; }
    public string? NombreProducto { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? IdProceso { get; set; }
}

[ExcludeFromCodeCoverage]
public class DtoComienzoResponse
{
    public long IdProceso { get; set; }
    public string? NombreProceso { get; set; }
}
