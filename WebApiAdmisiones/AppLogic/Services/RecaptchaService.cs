using System.Net.Http.Json;
using System.Text.Json;
using AppLogic.IServices;
using Utilities;

namespace AppLogic.Services
{
    public class RecaptchaService : IRecaptchaService
    {
        private const string DefaultProjectId = "admisiones-457619";
        private readonly HttpClient _httpClient;

        public RecaptchaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<OperationResult<bool>> ValidarAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return OperationResult<bool>.IsFailed(
                    "REG_CAPTCHA_01",
                    nameof(ValidarAsync),
                    "El parámetro captcha es obligatorio.",
                    400,
                    false);
            }

            var siteKey = Environment.GetEnvironmentVariable("RECAPTCHA_SITE_KEY");
            var apiKey = Environment.GetEnvironmentVariable("RECAPTCHA_API_KEY");
            var projectId = DefaultProjectId;
            var minimumScore = GetMinimumScore();

            if (string.IsNullOrWhiteSpace(siteKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                return OperationResult<bool>.IsFailed(
                    "REG_CAPTCHA_02",
                    nameof(ValidarAsync),
                    "No se encontró la configuración de reCAPTCHA.",
                    500,
                    false);
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"https://recaptchaenterprise.googleapis.com/v1/projects/{projectId}/assessments?key={apiKey}",
                    new
                    {
                        @event = new
                        {
                            token,
                            siteKey,
                            expectedAction = "USER_ACTION"
                        }
                    });

                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return OperationResult<bool>.IsFailed(
                        "REG_CAPTCHA_03",
                        nameof(ValidarAsync),
                        $"Error al validar captcha contra Google: {body}",
                        502,
                        false);
                }

                using var json = JsonDocument.Parse(body);
                if (!json.RootElement.TryGetProperty("riskAnalysis", out var riskAnalysis)
                    || !riskAnalysis.TryGetProperty("score", out var scoreElement)
                    || !scoreElement.TryGetDouble(out var score))
                {
                    return OperationResult<bool>.IsFailed(
                        "REG_CAPTCHA_04",
                        nameof(ValidarAsync),
                        "Error al validar el captcha contra Google.",
                        400,
                        false);
                }

                if (score < minimumScore)
                {
                    return OperationResult<bool>.IsFailed(
                        "REG_CAPTCHA_05",
                        nameof(ValidarAsync),
                        "Error al validar el captcha. Actividad sospechosa detectada.",
                        400,
                        false);
                }

                return OperationResult<bool>.Ok(true, nameof(ValidarAsync));
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.IsFailed(
                    "REG_CAPTCHA_99",
                    nameof(ValidarAsync),
                    $"Error al validar captcha: {ex.Message}",
                    500,
                    false);
            }
        }

        private static double GetMinimumScore()
        {
            var rawValue = Environment.GetEnvironmentVariable("RECAPTCHA_SCORE") ?? "0.5";

            return double.TryParse(rawValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var score)
                ? score
                : 0.5;
        }
    }
}
