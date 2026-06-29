using AppLogic.Dtos.Autenticacion;

namespace AppLogic.IServices.Autenticacion;

public interface ILoginFlowService
{
    Task<DtoLoginFlowResult> EjecutarAsync(DtoAuthRequest request, string ipAddress, double recaptchaScore);
}
