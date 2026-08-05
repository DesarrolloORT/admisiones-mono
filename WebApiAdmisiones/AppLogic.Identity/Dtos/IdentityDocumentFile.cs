namespace AppLogic.Identity.Dtos;

/// <summary>Archivo del documento de identidad (frente o dorso).</summary>
public class IdentityDocumentFile
{
    /// <summary>Nombre del archivo con el que se persistió la imagen.</summary>
    public string? FileName { get; set; }

    /// <summary>Contenido binario de la imagen.</summary>
    public byte[]? Content { get; set; }
}
