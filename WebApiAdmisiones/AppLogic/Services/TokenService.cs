using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using AppLogic.IServices;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using BusinessLogic.Entities;


namespace AppLogic.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateAccessToken(Persona user)
        {
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.CodigoPersona.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.CodigoPersona.ToString())
        };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET_KEY"))); 

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JWT_ISSUER_TOKEN"),
                audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE_TOKEN"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES"))),
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
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
