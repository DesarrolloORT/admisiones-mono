using BusinessLogic.Entities;

namespace AppLogic.IServices.Autenticacion
{
    public interface ITokenService
    {
        string GenerateAccessToken(Persona user);
        string GenerateRefreshToken();
        string HashToken(string token);
    }
}
