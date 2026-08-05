using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using BusinessLogic.Entities;
using AppLogic.Authentication.Interfaces;
using AppLogic.Authentication.Security;


namespace AppLogic.Authentication.Services;

public class TokenService : ITokenService
{
    public string GenerateAccessToken(Persona user)
    {
        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, user.CodigoPersona.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.CodigoPersona.ToString())
    };

        var secretKey = JwtConfiguration.GetRequiredEnvironmentVariable("JWT_SECRET_KEY");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey)); 

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Environment.GetEnvironmentVariable("JWT_ISSUER_TOKEN_ADMISIONES"),
            audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE_TOKEN_ADMISIONES"),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        return TokenHashing.HashSha256Base64(token);
    }

    private static double GetJwtExpirationMinutes()
    {
        return JwtConfiguration.GetRequiredDouble("JWT_EXPIRE_MINUTES_ADMISIONES");
    }
}
