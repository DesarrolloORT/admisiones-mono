namespace BusinessLogic.IServices
{
    /// <summary>
    /// Servicio para gestión de refresh tokens en base de datos.
    /// </summary>
    public interface IRefreshTokenService
    {
        /// <summary>Guarda un nuevo refresh token (revocando el anterior del mismo usuario/sistema).</summary>
        Task SaveRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash, DateTime expiresAt);

        /// <summary>Valida que el refresh token exista y no haya expirado ni sido revocado.</summary>
        Task<bool> ValidateRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash);

        /// <summary>Revoca (elimina) un refresh token específico.</summary>
        Task RevokeRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash);
    }
}
