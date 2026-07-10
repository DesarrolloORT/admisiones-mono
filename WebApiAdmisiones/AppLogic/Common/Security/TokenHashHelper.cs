using System.Security.Cryptography;
using System.Text;

namespace AppLogic.Common.Security;

/// <summary>
/// Hash SHA256 → Base64 compartido. Mismo algoritmo que ya usaban
/// <c>TokenService.HashToken</c> y <c>PasswordActivationService.HashToken</c>.
/// No usar para el hash hex de códigos 2FA de <c>DosFactoresAuthService</c> (formato distinto).
/// </summary>
public static class TokenHashHelper
{
    public static string HashSha256Base64(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(bytes);
    }
}
