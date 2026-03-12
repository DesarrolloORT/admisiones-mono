using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.IServices
{
    /// <summary>
    /// Interfaz para el servicio de gestión de refresh tokens.
    /// </summary>
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Guarda un nuevo refresh token en la base de datos, revocando los tokens activos anteriores.
        /// </summary>
        /// <param name="codigoPersona">Código de la persona.</param>
        /// <param name="sistema">Sistema que genera el token (ej: "EMPLEOSWEB").</param>
        /// <param name="tokenHash">Hash del refresh token.</param>
        /// <param name="expiresAt">Fecha y hora de expiración del token.</param>
        /// <returns>Task que representa la operación asíncrona.</returns>
        Task SaveRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash, DateTime expiresAt);

        /// <summary>
        /// Valida si un refresh token es válido y está activo.
        /// </summary>
        /// <param name="codigoPersona">Código de la persona.</param>
        /// <param name="sistema">Sistema del token.</param>
        /// <param name="tokenHash">Hash del refresh token a validar.</param>
        /// <returns>True si el token es válido y activo, false en caso contrario.</returns>
        Task<bool> ValidateRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash);

        /// <summary>
        /// Revoca un refresh token específico.
        /// </summary>
        /// <param name="codigoPersona">Código de la persona.</param>
        /// <param name="sistema">Sistema del token.</param>
        /// <param name="tokenHash">Hash del token a revocar.</param>
        /// <returns>Task que representa la operación asíncrona.</returns>
        Task RevokeRefreshTokenAsync(long codigoPersona, string sistema, string tokenHash);
    }
}
