using AppLogic.IServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
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
        private readonly IWebHostEnvironment _environment;

        public RequireCaptchaFilter(IRecaptchaService recaptchaService, IWebHostEnvironment environment)
        {
            _recaptchaService = recaptchaService;
            _environment = environment;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            //if (_environment.IsDevelopment() || _environment.IsEnvironment("LocalHost"))
            //{
            //    await next();
            //    return;
            //}

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
