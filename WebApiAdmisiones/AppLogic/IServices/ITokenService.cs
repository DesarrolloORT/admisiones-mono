using BusinessLogic.Entities;

namespace AppLogic.IServices;

/// <summary>
/// Abstracción para la generación de tokens JWT, permitiendo que AppLogic
/// permanezca independiente de la infraestructura de autenticación del host.
/// </summary>
public interface ITokenService
{
    /// <summary>Genera un token de acceso JWT firmado para la persona indicada.</summary>
    string GenerateAccessToken(Persona user);

    /// <summary>Genera un refresh token usando criptografía aleatoria segura.</summary>
    string GenerateRefreshToken();

    /// <summary>Calcula el hash SHA-256 de un token (para almacenamiento seguro en BD).</summary>
    string HashToken(string token);
}
