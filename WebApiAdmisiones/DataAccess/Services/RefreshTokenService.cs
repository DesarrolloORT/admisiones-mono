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
        /// Guarda (o reemplaza) el refresh token de la persona/sistema de forma atómica.
        /// </summary>
        /// <remarks>
        /// La entidad <see cref="RefreshToken"/> tiene clave primaria compuesta (CodigoPersona, Sistema),
        /// por lo que solo puede existir un token por combinación. En lugar de borrar el token anterior
        /// y luego insertar el nuevo (dos SaveChanges: si el segundo fallaba, la persona quedaba sin token),
        /// se hace un "upsert" sobre el mismo registro y se persiste con un único <c>SaveChangesAsync</c>.
        /// Así la operación es atómica: o queda el token nuevo, o se conserva intacto el anterior.
        /// Un Remove + Add de la misma clave en un solo contexto no es posible: la entidad marcada como
        /// eliminada sigue rastreada y el Add lanzaría un conflicto de identidad del change tracker.
        /// </remarks>
        public async Task SaveRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash, DateTime expiresAt)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.CodigoPersona == codigoPersona && rt.Sistema == sistema);

            if (token == null)
            {
                token = new RefreshToken
                {
                    CodigoPersona = codigoPersona,
                    Sistema = sistema
                };
                _context.RefreshTokens.Add(token);
            }

            // Upsert: se actualiza el mismo registro (o se completa el nuevo) y se activa.
            token.TokenHash = tokenHash;
            token.ExpiresAt = expiresAt;
            token.CreatedAt = DateTime.UtcNow;
            token.IsActive = "SI";
            token.RevokedAt = null;
            token.RemplaceByTokenId = null;
            token.FechaIngreso = DateTime.Now;
            token.HoraIngreso = DateTime.Now.ToString("HHmmss");
            token.UsuarioIngreso = codigoPersona.ToString();

            // Único punto de escritura: EF lo ejecuta en una transacción implícita (UPDATE o INSERT).
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
