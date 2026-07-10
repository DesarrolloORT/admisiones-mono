using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using BusinessLogic.Entities;
using AppLogic.Autenticacion.Interfaces;


namespace AppLogic.Autenticacion.Services
{
    public class TokenService : ITokenService
    {
        public string GenerateAccessToken(Persona user)
        {
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.CodigoPersona.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.CodigoPersona.ToString())
        };

            var secretKey = ObtenerVariableEntornoRequerida("JWT_SECRET_KEY");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)); 

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JWT_ISSUER_TOKEN_ADMISIONES"),
                audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE_TOKEN_ADMISIONES"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(ObtenerMinutosExpiracionJwt()),
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
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        private static double ObtenerMinutosExpiracionJwt()
        {
            var rawValue = Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES_ADMISIONES");
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                throw new InvalidOperationException("La variable de entorno JWT_EXPIRE_MINUTES_ADMISIONES no está configurada.");
            }

            if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes))
            {
                throw new InvalidOperationException("La variable de entorno JWT_EXPIRE_MINUTES_ADMISIONES tiene un valor inválido.");
            }

            return minutes;
        }

        private static string ObtenerVariableEntornoRequerida(string variableName)
        {
            var value = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"La variable de entorno {variableName} no está configurada.");
            }

            return value;
        }
    }
}
