using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Utilities;
using WebApiAdmisiones.Security.Captcha;

namespace WebApiAdmisiones.Security.Captcha
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireCaptchaAttribute : TypeFilterAttribute
    {
        public RequireCaptchaAttribute(
            CaptchaValidationMode mode = CaptchaValidationMode.RequireMinimumScore)
            : base(typeof(RequireCaptchaFilter))
        {
            Arguments = [mode];
        }
    }

    public sealed class RequireCaptchaFilter : IAsyncActionFilter
    {
        public const string HeaderName = "X-Captcha-Token";

        private readonly IRecaptchaService _recaptchaService;
        private readonly CaptchaValidationMode _mode;

        public RequireCaptchaFilter(IRecaptchaService recaptchaService, CaptchaValidationMode mode)
        {
            _recaptchaService = recaptchaService;
            _mode = mode;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var token = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();
            var validation = await _recaptchaService.ValidarConScoreAsync(token ?? string.Empty, "login");
            if (!validation.Success)
            {
                var errorCode = _mode == CaptchaValidationMode.RequireMinimumScore
                    ? MapAuthErrorToRegistrationError(validation.ErrorCode)
                    : validation.ErrorCode;

                SetFailureResult(
                    context,
                    errorCode,
                    validation.Message,
                    validation.HttpCode);
                return;
            }

            context.HttpContext.SetRecaptchaScore(validation.Data);

            if (_mode == CaptchaValidationMode.RequireMinimumScore
                && validation.Data < RecaptchaSettings.MinimumScore)
            {
                SetFailureResult(
                    context,
                    "REG_CAPTCHA_05",
                    "Error al validar el captcha. Actividad sospechosa detectada.",
                    400);
                return;
            }

            await next();
        }

        private static void SetFailureResult(
            ActionExecutingContext context,
            string errorCode,
            string message,
            int httpCode)
        {
            var result = OperationResult<object>.IsFailed(
                errorCode,
                nameof(RequireCaptchaFilter),
                message,
                httpCode);

            context.Result = new ObjectResult(result)
            {
                StatusCode = httpCode
            };
        }

        private static string MapAuthErrorToRegistrationError(string errorCode) =>
            errorCode switch
            {
                "AUTH_CAPTCHA_01" => "REG_CAPTCHA_01",
                "AUTH_CAPTCHA_02" => "REG_CAPTCHA_02",
                "AUTH_CAPTCHA_03" => "REG_CAPTCHA_03",
                "AUTH_CAPTCHA_04" => "REG_CAPTCHA_04",
                "AUTH_CAPTCHA_05" => "REG_CAPTCHA_04",
                "AUTH_CAPTCHA_99" => "REG_CAPTCHA_99",
                _ => "REG_CAPTCHA_99"
            };
    }
}
