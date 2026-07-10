using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Registro.Dtos;

[ExcludeFromCodeCoverage]
public sealed class DtoRegistroDocumentoImagenesTemporales
{
    public string TipoDocumento { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public DtoRegistroDocumentoArchivoTemporal DocumentoFrente { get; set; } = new();
    public DtoRegistroDocumentoArchivoTemporal? CaraPersona { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[ExcludeFromCodeCoverage]
public sealed class DtoRegistroDocumentoArchivoTemporal
{
    public byte[] Archivo { get; set; } = [];
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
}
