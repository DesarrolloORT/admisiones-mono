using System.Text.Json.Serialization;

namespace AppLogic.Dtos.EncuestaInicial
{
    public sealed class DtoObtenerEncuestaInicialResponse
    {
        public bool TieneDerechoEncuesta { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DtoEncuestaInicialLectura? Encuesta { get; set; }
    }

    public sealed class DtoEncuestaInicialLectura : DtoGuardarEncuestaInicialRequest
    {
        public long IdEncuestaIni { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
