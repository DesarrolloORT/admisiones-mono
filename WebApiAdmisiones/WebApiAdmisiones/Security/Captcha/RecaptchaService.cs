using System.Text.Json;
using Utilities;

namespace WebApiAdmisiones.Security.Captcha
{
    public class RecaptchaService : IRecaptchaService
    {
        private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";
        private const string SecretKeyEnvironmentVariable = "RECAPTCHA_SECRET_KEY";

        private readonly HttpClient _httpClient;

        public RecaptchaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<OperationResult<double>> ValidarConScoreAsync(string token, string expectedAction = "login")
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return OperationResult<double>.IsFailed(
                    "AUTH_CAPTCHA_01",
                    nameof(ValidarConScoreAsync),
                    "El parametro captcha es obligatorio.",
                    400,
                    0d);
            }

            var secretKey = Environment.GetEnvironmentVariable(SecretKeyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return OperationResult<double>.IsFailed(
                    "AUTH_CAPTCHA_02",
                    nameof(ValidarConScoreAsync),
                    $"No se encontro la configuracion de reCAPTCHA v3. Defini {SecretKeyEnvironmentVariable}.",
                    500,
                    0d);
            }

            try
            {
                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = secretKey,
                    ["response"] = token
                });

                var response = await _httpClient.PostAsync(VerifyUrl, content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return OperationResult<double>.IsFailed(
                        "AUTH_CAPTCHA_03",
                        nameof(ValidarConScoreAsync),
                        $"Error al validar captcha contra Google: {body}",
                        502,
                        0d);
                }

                using var json = JsonDocument.Parse(body);
                var root = json.RootElement;

                if (!root.TryGetProperty("success", out var successElement)
                    || !successElement.GetBoolean())
                {
                    return OperationResult<double>.IsFailed(
                        "AUTH_CAPTCHA_04",
                        nameof(ValidarConScoreAsync),
                        $"Google no valido el captcha. {GetErrorCodes(root)}",
                        400,
                        0d);
                }

                if (!root.TryGetProperty("score", out var scoreElement)
                    || !scoreElement.TryGetDouble(out var score))
                {
                    return OperationResult<double>.IsFailed(
                        "AUTH_CAPTCHA_04",
                        nameof(ValidarConScoreAsync),
                        "Error al obtener el score del captcha.",
                        400,
                        0d);
                }

                var action = root.TryGetProperty("action", out var actionElement)
                    ? actionElement.GetString()
                    : null;

                if (!string.IsNullOrWhiteSpace(expectedAction)
                    && !string.Equals(action, expectedAction, StringComparison.Ordinal))
                {
                    return OperationResult<double>.IsFailed(
                        "AUTH_CAPTCHA_05",
                        nameof(ValidarConScoreAsync),
                        "La accion del captcha no coincide con la accion esperada.",
                        400,
                        0d);
                }

                return OperationResult<double>.Ok(score, nameof(ValidarConScoreAsync));
            }
            catch (Exception ex)
            {
                return OperationResult<double>.IsFailed(
                    "AUTH_CAPTCHA_99",
                    nameof(ValidarConScoreAsync),
                    $"Error al validar captcha: {ex.Message}",
                    500,
                    0d);
            }
        }

        private static string GetErrorCodes(JsonElement root)
        {
            if (!root.TryGetProperty("error-codes", out var errorCodes)
                || errorCodes.ValueKind != JsonValueKind.Array)
            {
                return string.Empty;
            }

            var codes = errorCodes
                .EnumerateArray()
                .Select(code => code.GetString())
                .Where(code => !string.IsNullOrWhiteSpace(code));

            return $"Codigos: {string.Join(", ", codes)}";
        }
    }
}
