using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AppLogic.IServices;
using Microsoft.IdentityModel.Tokens;

namespace AppLogic.Services
{
    /// <summary>
    /// Implementación del servicio para generar tokens de autenticación con APIs internas.
    /// NOTA: Este servicio SOLO genera tokens. La validación se realiza en la API destino (Inscripciones y Pagos).
    /// </summary>
    public class TokenServiceInternalApi : ITokenServiceInternalApi
    {
        private const string ServiceClaimType = "service_name";
        private const string TargetApiClaimType = "target_api";
        private const string ScopeClaimType = "scope";

        /// <summary>
        /// Genera un token JWT firmado para autenticación de servicio a servicio.
        /// </summary>
        /// <param name="targetApi">API destino (ej: "api-inscripciones-pagos").</param>
        /// <param name="scopes">Permisos solicitados (ej: "inscripciones.write", "pagos.read").</param>
        /// <returns>Token JWT firmado.</returns>
        public string GenerateServiceToken(string targetApi, params string[] scopes)
        {
            var claims = new[]
            {
                // Identifica al servicio que hace la llamada
                new Claim(ServiceClaimType, "api-admisiones"),

                // API destino
                new Claim(TargetApiClaimType, targetApi),

                // Scopes solicitados (separados por espacios, como OAuth2)
                new Claim(ScopeClaimType, string.Join(" ", scopes)),

                // JTI para prevenir replay attacks (opcional pero recomendado)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var secretKey = ObtenerVariableEntornoRequerida("SECRET_KEY_API_INSCR_PAGOS");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "api-admisiones",
                audience: targetApi,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5), // Corta duración: solo para la llamada
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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
