using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Registration.Dtos;

/// <summary>
/// Datos extraídos del documento adjunto por Azure Document Intelligence. Es la traducción del
/// <c>ReconocimientoDocumentoResponse</c> de <c>Core/AzureService</c>: ese tipo es de un submódulo
/// compartido con otros proyectos ORT y no se puede renombrar, así que el límite anticorrupción
/// vive acá.
/// </summary>
[ExcludeFromCodeCoverage]
public class DocumentRecognitionResponse
{
    /// <summary>El reconocimiento no fue concluyente: el front debe pedir revisión manual de los datos.</summary>
    public bool RequiresReview { get; set; }

    /// <summary>Campos leídos del documento.</summary>
    public RecognizedDocumentFields Fields { get; set; } = new();

    /// <summary>Recorte de la foto del titular, cuando el documento la trae.</summary>
    public RecognizedFace? PersonFace { get; set; }

    /// <summary>Advertencias del reconocimiento (campos dudosos, baja confianza).</summary>
    public IReadOnlyList<string> Warnings { get; set; } = [];
}

/// <summary>Campos que Azure logró extraer del documento.</summary>
[ExcludeFromCodeCoverage]
public class RecognizedDocumentFields
{
    /// <summary>CI, PS o DE.</summary>
    public string? DocumentType { get; set; }

    /// <summary>Número de documento.</summary>
    public string? DocumentNumber { get; set; }

    /// <summary>Primer nombre.</summary>
    public string? FirstName { get; set; }

    /// <summary>Segundo nombre.</summary>
    public string? MiddleName { get; set; }

    /// <summary>Primer apellido.</summary>
    public string? FirstSurname { get; set; }

    /// <summary>Segundo apellido.</summary>
    public string? SecondSurname { get; set; }

    /// <summary>Fecha de nacimiento.</summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>Lugar de nacimiento.</summary>
    public string? BirthPlace { get; set; }

    /// <summary>Departamento de emisión, solo en cédula uruguaya.</summary>
    public string? State { get; set; }

    /// <summary>Sexo: M o F.</summary>
    public string? Sex { get; set; }

    /// <summary>Fecha de vencimiento del documento.</summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>Nacionalidad declarada en el documento.</summary>
    public string? Nationality { get; set; }
}

/// <summary>Imagen de la cara del titular recortada del documento.</summary>
[ExcludeFromCodeCoverage]
public class RecognizedFace
{
    /// <summary>Bytes de la imagen recortada.</summary>
    public byte[] Content { get; set; } = [];

    /// <summary>Nombre sugerido del archivo.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Tipo MIME de la imagen.</summary>
    public string ContentType { get; set; } = "application/octet-stream";
}
