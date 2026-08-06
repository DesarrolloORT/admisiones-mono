using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Scholarships.Dtos;

/// <summary>Archivo adjunto de la declaración jurada listo para descargar.</summary>
[ExcludeFromCodeCoverage]
public class FileDownload
{
    /// <summary>Contenido del archivo.</summary>
    public byte[] Content { get; set; } = [];

    /// <summary>Nombre con el que se descarga, ya con extensión.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Tipo MIME resuelto desde la extensión.</summary>
    public string ContentType { get; set; } = "application/octet-stream";
}
