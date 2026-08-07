using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Utilities;
using WebApiAdmisiones.Security.Captcha;

namespace WebApiAdmisiones.Security.Captcha
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireCaptchaAttribute : TypeFilterAttribute
    {
        public RequireCaptchaAttribute()
            : this(CaptchaActions.Login, CaptchaValidationMode.RequireMinimumScore)
        {
        }

        public RequireCaptchaAttribute(CaptchaValidationMode mode)
            : this(CaptchaActions.Login, mode)
        {
        }

        public RequireCaptchaAttribute(
            string expectedAction,
            CaptchaValidationMode mode = CaptchaValidationMode.RequireMinimumScore)
            : base(typeof(RequireCaptchaFilter))
        {
            Arguments = [mode, expectedAction];
        }
    }

    /// <summary>
    /// Los VALORES son contrato con el front: viajan a reCAPTCHA y tienen que coincidir con la
    /// action que el front dispara. Se renombraron los identificadores, no los literales.
    /// </summary>
    public static class CaptchaActions
    {
        public const string Login = "login";
        public const string RecoverPassword = "RecoverPassword";
        public const string EvaluateDocument = "EvaluateDocument";
        public const string VerifyIdentity = "VerifyIdentity";
        public const string AnalyzeAttachment = "AnalyzeAttachment";
        public const string ConfirmNewPerson = "ConfirmNewPerson";
        public const string ConfirmRegistrationRequest = "ConfirmRegistrationRequest";
    }

    public sealed class RequireCaptchaFilter : IAsyncActionFilter
    {
        public const string HeaderName = "X-Captcha-Token";

        private readonly IRecaptchaService _recaptchaService;
        private readonly IConfiguration _configuration;
        private readonly CaptchaValidationMode _mode;
        private readonly string _expectedAction;

        public RequireCaptchaFilter(
            IRecaptchaService recaptchaService,
            IConfiguration configuration,
            CaptchaValidationMode mode,
            string expectedAction = CaptchaActions.Login)
        {
            _recaptchaService = recaptchaService;
            _configuration = configuration;
            _mode = mode;
            _expectedAction = string.IsNullOrWhiteSpace(expectedAction)
                ? CaptchaActions.Login
                : expectedAction;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var token = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();
            var validation = await _recaptchaService.ValidarConScoreAsync(token ?? string.Empty, _expectedAction);
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

            var minimumScore = _configuration.GetValue<double>("RECAPTCHA_SCORE", 0.5);
            if (_mode == CaptchaValidationMode.RequireMinimumScore
                && validation.Data < minimumScore)
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
