using BusinessLogic.Entities;

namespace AppLogic.IServices
{
    public interface ITokenService
    {
        string GenerateAccessToken(Persona user);
        string GenerateRefreshToken();
        string HashToken(string token);
    }
}
