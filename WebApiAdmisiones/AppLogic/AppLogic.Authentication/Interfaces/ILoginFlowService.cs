using AppLogic.Authentication.Dtos;

namespace AppLogic.Authentication.Interfaces;

public interface ILoginFlowService
{
    Task<LoginFlowResult> ExecuteAsync(AuthRequest request, string ipAddress, double recaptchaScore);
}
