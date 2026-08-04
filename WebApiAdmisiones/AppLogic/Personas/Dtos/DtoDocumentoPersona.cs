using System.Text.Json.Serialization;

namespace AppLogic.Personas.Dtos;

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

