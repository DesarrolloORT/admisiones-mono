using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.Catalogos.Dtos
{
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
        public List<DtoCarreraResponse> Productos { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class DtoCarreraResponse
    {
        public long IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? IdProceso { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? TieneSeminario { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class DtoComienzoResponse
    {
        public long IdProceso { get; set; }
        public string? NombreProceso { get; set; }
    }
}
