using System.Text.Json.Serialization;

namespace AppLogic.Inscripciones.Encuesta.Responses
{
    public sealed class DtoObtenerEncuestaInicialResponse
    {
        public bool TieneDerechoEncuesta { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DtoEncuestaInicialLectura? Encuesta { get; set; }
    }
}
