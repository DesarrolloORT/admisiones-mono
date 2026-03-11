using BusinessLogic.IServices;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using BusinessLogic.Entities;

namespace DataAccess.Services
{
    /// <summary>
    /// Implementación de <see cref="IRefreshTokenService"/> usando EF Core con ModelContext.
    /// </summary>
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly ModelContext _context;

        public RefreshTokenService(ModelContext context)
        {
            _context = context;
        }

        public async Task SaveRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash, DateTime expiresAt)
        {
            // Eliminar token anterior del mismo usuario/sistema (one active token per user per system)
            var existingToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.CodigoPersona == codigoPersona && rt.Sistema == sistema);

            if (existingToken != null)
                _context.RefreshTokens.Remove(existingToken);

            var refreshToken = new RefreshToken
            {
                CodigoPersona = codigoPersona,
                Sistema = sistema,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                IsActive = "SI",
                UsuarioIngreso = codigoPersona.ToString()
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ValidateRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt =>
                    rt.CodigoPersona == codigoPersona &&
                    rt.Sistema == sistema &&
                    rt.TokenHash == tokenHash &&
                    rt.IsActive == "SI" &&
                    rt.ExpiresAt > DateTime.UtcNow);

            return token != null;
        }

        public async Task RevokeRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt =>
                    rt.CodigoPersona == codigoPersona &&
                    rt.Sistema == sistema &&
                    rt.TokenHash == tokenHash);

            if (token != null)
            {
                _context.RefreshTokens.Remove(token);
                await _context.SaveChangesAsync();
            }
        }
    }
}
