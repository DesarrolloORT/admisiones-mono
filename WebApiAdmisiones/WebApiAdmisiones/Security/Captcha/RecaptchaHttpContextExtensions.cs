using Microsoft.AspNetCore.Http;

namespace WebApiAdmisiones.Security.Captcha
{
    public static class RecaptchaHttpContextExtensions
    {
        private const string RecaptchaScoreItemKey = "RecaptchaScore";

        public static void SetRecaptchaScore(this HttpContext httpContext, double score)
        {
            httpContext.Items[RecaptchaScoreItemKey] = score;
        }

        public static double? GetRecaptchaScore(this HttpContext httpContext)
        {
            return httpContext.Items.TryGetValue(RecaptchaScoreItemKey, out var value)
                && value is double score
                    ? score
                    : null;
        }
    }
}
