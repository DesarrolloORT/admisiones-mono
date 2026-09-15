using BusinessLogic.Entities;

namespace AppLogic.Authentication.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Persona user);
    string GenerateRefreshToken();
    string HashToken(string token);
}
