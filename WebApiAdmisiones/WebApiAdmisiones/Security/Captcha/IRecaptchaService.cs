using Utilities;

namespace WebApiAdmisiones.Security.Captcha
{
    public interface IRecaptchaService
    {
        Task<OperationResult<double>> ValidarConScoreAsync(string token, string expectedAction = "login");
    }
}
