using AppLogic.DTOs;

namespace WebApiAdmisiones.Security
{
    public interface ILoginFlowService
    {
        Task<LoginFlowResult> EjecutarAsync(AuthRequest request, string ipAddress, string captchaToken);
    }
}
