using BusinessLogic.Entities;
using BusinessLogic.IServices;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    /// <summary>
    /// Servicio para gestionar los refresh tokens en la base de datos.
    /// </summary>
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly ModelContext _context;

        public RefreshTokenService(ModelContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Guarda un nuevo refresh token en la base de datos, revocando los tokens activos anteriores.
        /// </summary>
        public async Task SaveRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash, DateTime expiresAt)
        {
            // Buscar y eliminar el token existente con la misma clave compuesta
            var existingToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.CodigoPersona == codigoPersona && rt.Sistema == sistema);

            if (existingToken != null)
            {
                // Eliminar el token anterior
                _context.RefreshTokens.Remove(existingToken);
                await _context.SaveChangesAsync();
            }

            // Guardar el nuevo refresh token
            var refreshTokenEntity = new RefreshToken
            {
                CodigoPersona = codigoPersona,
                Sistema = sistema,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                IsActive = "SI",
                FechaIngreso = DateTime.Now,
                HoraIngreso = DateTime.Now.ToString("HHmmss"),
                UsuarioIngreso = codigoPersona.ToString()
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Valida si un refresh token es válido y está activo.
        /// </summary>
        public async Task<bool> ValidateRefreshTokenAsync(string sistema, string tokenHash)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Sistema == sistema &&
                                          rt.TokenHash == tokenHash &&
                                          rt.IsActive == "SI" &&
                                          rt.ExpiresAt > DateTime.UtcNow);

            return token != null;
        }

        /// <summary>
        /// Obtiene el código de persona asociado a un refresh token activo y no expirado.
        /// </summary>
        public async Task<long?> GetCodigoPersonaByRefreshTokenAsync(string sistema, string tokenHash)
        {
            var token = await _context.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.Sistema == sistema &&
                                           rt.TokenHash == tokenHash &&
                                           rt.IsActive == "SI" &&
                                           rt.ExpiresAt > DateTime.UtcNow);

            return token?.CodigoPersona;
        }

        /// <summary>
        /// Revoca un refresh token específico.
        /// </summary>
        public async Task RevokeRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.CodigoPersona == codigoPersona &&
                                          rt.Sistema == sistema &&
                                          rt.TokenHash == tokenHash);

            if (token != null)
            {
                token.IsActive = "NO";
                token.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
