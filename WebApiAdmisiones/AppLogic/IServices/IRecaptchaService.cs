using Utilities;

namespace AppLogic.IServices
{
    public interface IRecaptchaService
    {
        Task<OperationResult<bool>> ValidarAsync(string token);

        /// <summary>
        /// Valida el token reCAPTCHA v3 Enterprise y retorna el score numérico (0.0–1.0).
        /// No aplica threshold interno; la decisión queda en mano del llamador.
        /// </summary>
        Task<OperationResult<double>> ValidarConScoreAsync(string token, string expectedAction = "login");
    }
}
