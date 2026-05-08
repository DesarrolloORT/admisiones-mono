using Utilities;

namespace AppLogic.IServices
{
    public interface IRecaptchaService
    {
        Task<OperationResult<bool>> ValidarAsync(string token);
    }
}
