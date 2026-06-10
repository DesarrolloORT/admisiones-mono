using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs;

[ExcludeFromCodeCoverage]
public sealed class RegistroDocumentoImagenesTemporales
{
    public string TipoDocumento { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public RegistroDocumentoArchivoTemporal DocumentoFrente { get; set; } = new();
    public RegistroDocumentoArchivoTemporal? CaraPersona { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[ExcludeFromCodeCoverage]
public sealed class RegistroDocumentoArchivoTemporal
{
    public byte[] Archivo { get; set; } = [];
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
}
