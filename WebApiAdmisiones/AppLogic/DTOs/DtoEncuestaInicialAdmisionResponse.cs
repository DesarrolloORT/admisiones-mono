using AppLogic.DevartDTOs;
using System.Text.Json.Serialization;

namespace AppLogic.DTOs
{
    public class DtoEncuestaInicialAdmisionResponse
    {
        public bool TieneDerechoEncuesta { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DtoEncuestaIniAdmisionDevart? Encuesta { get; set; }
    }
}
