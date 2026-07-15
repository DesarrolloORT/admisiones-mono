using AppLogic.Autenticacion.Dtos;

namespace AppLogic.Autenticacion.Interfaces;

public interface ILoginFlowService
{
    Task<DtoLoginFlowResult> EjecutarAsync(DtoAuthRequest request, string ipAddress, double recaptchaScore);
}
