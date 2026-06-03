using AppLogic.DTOs;
using Utilities;

namespace WebApiAdmisiones.Security
{
    /// <summary>
    /// Servicio de autenticación de dos factores (2FA) por email.
    /// Gestiona la sesión temporal de verificación almacenada en Redis.
    /// </summary>
    public interface IDosFactoresAuthService
    {
        /// <summary>
        /// Inicia el flujo 2FA: genera un código, lo almacena en Redis y lo envía por email.
        /// </summary>
        /// <param name="pendingAuth">Datos de autenticación pendientes (tokens + persona).</param>
        /// <param name="email">Email del usuario al que se enviará el código.</param>
        /// <returns>Resultado con el SessionId de la sesión 2FA temporal.</returns>
        Task<OperationResult<DtoLogin2FARequired>> IniciarAsync(DtoAuthenticationResponse pendingAuth, string email);

        /// <summary>
        /// Verifica el código ingresado por el usuario.
        /// Si es válido, retorna los tokens para establecer las cookies de sesión.
        /// </summary>
        /// <param name="sessionId">Identificador de la sesión 2FA.</param>
        /// <param name="codigo">Código de verificación ingresado por el usuario.</param>
        /// <returns>Resultado con la respuesta de autenticación completa si el código es correcto.</returns>
        Task<OperationResult<DtoAuthenticationResponse>> VerificarCodigoAsync(string sessionId, string codigo);
    }
}
