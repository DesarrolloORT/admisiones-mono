using AppLogic.Identity.Dtos;
using System.Text.Json.Serialization;

namespace AppLogic.People.Dtos;

/// <summary>
/// Documento de identidad de la persona. Puede venir uno solo de los dos lados: se omite el que no
/// exista en vez de devolverlo en null.
/// </summary>
public class PersonIdentityDocumentResponse
{
    /// <summary>Frente del documento.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IdentityDocumentFile? Front { get; set; }

    /// <summary>Dorso del documento.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IdentityDocumentFile? Back { get; set; }

    /// <summary>Fecha de vencimiento del documento.</summary>
    public DateTime? ExpirationDate { get; set; }
}
