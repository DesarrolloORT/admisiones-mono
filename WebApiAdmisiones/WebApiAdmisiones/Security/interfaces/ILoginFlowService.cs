using AppLogic.DTOs;

namespace WebApiAdmisiones.Security.interfaces
{
    public interface ILoginFlowService
    {
        Task<LoginFlowResult> EjecutarAsync(AuthRequest request, string ipAddress, string captchaToken);
    }
}
