using System.Text.Json.Serialization;

#nullable enable annotations
#nullable disable warnings

namespace AppLogic.DevartDTOs
{
    // Extiende el DTO generado por Devart (no regenerar aca): DescripcionOferta solo se
    // completa para inscripciones de nivel 3 y 4, ver PersonService.MapearInscripcionFresco3y4.
    public partial class DtoVdInscripcionesFresco1y2Devart
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DescripcionOferta { get; set; }
    }
}
