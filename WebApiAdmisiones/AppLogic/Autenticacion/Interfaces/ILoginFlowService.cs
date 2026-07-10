using AppLogic.Autenticacion.Dtos;
using AppLogic.Autenticacion.Requests;

namespace AppLogic.Autenticacion.Interfaces;

public interface ILoginFlowService
{
    Task<DtoLoginFlowResult> EjecutarAsync(DtoAuthRequest request, string ipAddress, double recaptchaScore);
}
