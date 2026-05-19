using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.DTOs;

[ExcludeFromCodeCoverage]
public class DtoPasswordActivationSession
{
    public long CodigoPersona { get; set; }

    [JsonIgnore]
    public string? SessionToken { get; set; }

    public string Message { get; set; } = "Link validado correctamente.";
}
