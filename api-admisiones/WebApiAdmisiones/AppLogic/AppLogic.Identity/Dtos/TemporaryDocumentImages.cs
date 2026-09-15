using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Identity.Dtos;

/// <summary>
/// Imágenes del documento reconocido, guardadas en Redis entre el paso de evaluación y el alta
/// definitiva de la persona.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TemporaryDocumentImages
{
    /// <summary>Tipo de documento: CI, DE, PS o CC.</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Número de documento.</summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>Imagen del frente del documento.</summary>
    public TemporaryDocumentFile DocumentFront { get; set; } = new();

    /// <summary>Foto de la cara de la persona recortada del documento, si se pudo extraer.</summary>
    public TemporaryDocumentFile? PersonFace { get; set; }

    /// <summary>Fecha de vencimiento leída del documento.</summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>Momento en que se guardaron las imágenes.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Archivo temporal de una imagen del documento.</summary>
[ExcludeFromCodeCoverage]
public sealed class TemporaryDocumentFile
{
    /// <summary>Contenido binario del archivo.</summary>
    public byte[] Content { get; set; } = [];

    /// <summary>Nombre original del archivo.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Content type del archivo.</summary>
    public string ContentType { get; set; } = "application/octet-stream";
}
