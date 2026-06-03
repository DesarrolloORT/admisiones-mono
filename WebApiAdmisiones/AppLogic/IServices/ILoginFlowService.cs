using AppLogic.DTOs;

namespace AppLogic.IServices
{
    public interface ILoginFlowService
    {
        Task<LoginFlowResult> EjecutarAsync(AuthRequest request, string ipAddress, string captchaToken);
    }
}
