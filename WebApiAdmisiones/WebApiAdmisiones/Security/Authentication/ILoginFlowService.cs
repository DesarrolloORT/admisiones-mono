using AppLogic.DTOs;

namespace WebApiAdmisiones.Security.Authentication
{
    public interface ILoginFlowService
    {
        Task<LoginFlowResult> EjecutarAsync(AuthRequest request, string ipAddress, double recaptchaScore);
    }
}
