using AppLogic.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Utilities;

namespace WebApiAdmisiones.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireCaptchaAttribute : TypeFilterAttribute
    {
        public RequireCaptchaAttribute() : base(typeof(RequireCaptchaFilter))
        {
        }
    }

    public sealed class RequireCaptchaFilter : IAsyncActionFilter
    {
        public const string HeaderName = "X-Captcha-Token";

        private readonly IRecaptchaService _recaptchaService;

        public RequireCaptchaFilter(IRecaptchaService recaptchaService)
        {
            _recaptchaService = recaptchaService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var token = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();
            var validation = await _recaptchaService.ValidarAsync(token ?? string.Empty);
            if (!validation.Success)
            {
                var result = OperationResult<object>.IsFailed(
                    validation.ErrorCode,
                    nameof(RequireCaptchaFilter),
                    validation.Message,
                    validation.HttpCode);

                context.Result = new ObjectResult(result)
                {
                    StatusCode = validation.HttpCode
                };
                return;
            }

            await next();
        }
    }
}
