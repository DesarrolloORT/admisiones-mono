using AppLogic.DTOs;

namespace AppLogic.IServices.Autenticacion;

public interface ILoginFlowService
{
    Task<LoginFlowResult> EjecutarAsync(AuthRequest request, string ipAddress, double recaptchaScore);
}
