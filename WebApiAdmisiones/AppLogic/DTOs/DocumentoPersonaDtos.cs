using System.Text.Json.Serialization;

namespace AppLogic.DTOs
{
    public class DocumentoPersonaResponse
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DocumentoPersonaArchivoDto? Frente { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DocumentoPersonaArchivoDto? Dorso { get; set; }

        public DateTime? FechaVencimiento { get; set; }
    }

    public class DocumentoPersonaArchivoDto
    {
        public string? NombreArchivo { get; set; }
        public byte[]? Archivo { get; set; }
    }

    public class DocumentoPersonaConsultaDto
    {
        public DocumentoPersonaArchivoDto? Archivo { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }
}
