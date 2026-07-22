using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Becas.Dtos;

[ExcludeFromCodeCoverage]
public class DtoArchivoDescarga
{
    public byte[] Archivo { get; set; } = [];
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
}
