using System.Globalization;

namespace WebApiAdmisiones.Security.Captcha
{
    public static class RecaptchaSettings
    {
        public static double MinimumScore
        {
            get
            {
                var rawValue = Environment.GetEnvironmentVariable("RECAPTCHA_SCORE") ?? "0.5";
                return double.TryParse(
                    rawValue,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var score)
                    ? score
                    : 0.5;
            }
        }
    }
}
