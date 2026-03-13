using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AppLogic.DTOs
{
    /// <summary>
    /// DTO para la respuesta de autenticación exitosa.
    /// Los tokens se envían como cookies HttpOnly seguras y NO se incluyen en el body.
    /// </summary>
    [ExcludeFromCodeCoverage]
public class DTOAuthenticationResponse
{
        /// <summary>
        /// Información de la persona autenticada.
        /// </summary>
        public required DTOPersonaAuth Persona { get; set; }
    
        /// <summary>
        /// Mensaje informativo sobre el login exitoso.
        /// </summary>
    public string Message { get; set; } = "Autenticación exitosa. Los tokens han sido establecidos como cookies seguras.";

    // NOTA DE SEGURIDAD: Los tokens NO se incluyen en el body de la respuesta
    // para prevenir exposición a ataques XSS. Se envían como cookies HttpOnly.

    // Propiedades internas solo para uso del controlador (no se serializan)

    [JsonIgnore]
    public string? AccessToken { get; set; }

    [JsonIgnore]
    public string? RefreshToken { get; set; }

    [JsonIgnore]
    public string? RefreshTokenHash { get; set; }
    }
}
