using System.Text.Json.Serialization;

namespace AppLogic.Dtos.Personas
{
    public class DtoDocumentoPersonaResponse
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DtoDocumentoPersonaArchivo? Frente { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DtoDocumentoPersonaArchivo? Dorso { get; set; }

        public DateTime? FechaVencimiento { get; set; }
    }

    public class DtoDocumentoPersonaArchivo
    {
        public string? NombreArchivo { get; set; }
        public byte[]? Archivo { get; set; }
    }

    public class DtoDocumentoPersonaConsulta
    {
        public DtoDocumentoPersonaArchivo? Archivo { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }
}
