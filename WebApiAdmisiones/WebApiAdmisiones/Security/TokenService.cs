using AppLogic.IServices;
using BusinessLogic.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace WebApiAdmisiones.Security;

public class TokenService : ITokenService
{
    private const string Issuer = "https://admisiones.ort.edu.uy";
    private static readonly TimeSpan AccessTokenTtl = TimeSpan.FromMinutes(15);

    /// <inheritdoc/>
    public string GenerateAccessToken(Persona user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.CodigoPersona.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.CodigoPersona.ToString())
        };

        var secret = Environment.GetEnvironmentVariable("JWT_Key_Admisiones")
            ?? throw new InvalidOperationException("JWT secret is not set. Please configure the 'JWT_Key_Admisiones' environment variable.");

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Issuer,
            claims: claims,
            expires: DateTime.UtcNow.Add(AccessTokenTtl),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc/>
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc/>
    public string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
