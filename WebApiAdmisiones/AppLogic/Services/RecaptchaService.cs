using System.Globalization;
using System.Text.Json;
using AppLogic.IServices;
using Utilities;

namespace AppLogic.Services
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

        public async Task<OperationResult<bool>> ValidarAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return OperationResult<bool>.IsFailed(
                    "REG_CAPTCHA_01",
                    nameof(ValidarAsync),
                    "El parametro captcha es obligatorio.",
                    400,
                    false);
            }

            var verification = await VerifyV3Async(token, "login");
            if (!verification.Success)
            {
                return OperationResult<bool>.IsFailed(
                    MapAuthErrorToRegistrationError(verification.ErrorCode),
                    nameof(ValidarAsync),
                    verification.Message,
                    verification.HttpCode,
                    false);
            }

            var minimumScore = GetMinimumScore();
            if (verification.Data!.Score < minimumScore)
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

            var verification = await VerifyV3Async(token, expectedAction);
            if (!verification.Success)
            {
                return OperationResult<double>.IsFailed(
                    verification.ErrorCode,
                    nameof(ValidarConScoreAsync),
                    verification.Message,
                    verification.HttpCode,
                    0d);
            }

            return OperationResult<double>.Ok(verification.Data!.Score, nameof(ValidarConScoreAsync));
        }

        private async Task<OperationResult<RecaptchaV3Verification>> VerifyV3Async(
            string token,
            string? expectedAction)
        {
            var secretKey = Environment.GetEnvironmentVariable(SecretKeyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return OperationResult<RecaptchaV3Verification>.IsFailed(
                    "AUTH_CAPTCHA_02",
                    nameof(VerifyV3Async),
                    $"No se encontro la configuracion de reCAPTCHA v3. Defini {SecretKeyEnvironmentVariable}.",
                    500);
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
                    return OperationResult<RecaptchaV3Verification>.IsFailed(
                        "AUTH_CAPTCHA_03",
                        nameof(VerifyV3Async),
                        $"Error al validar captcha contra Google: {body}",
                        502);
                }

                using var json = JsonDocument.Parse(body);
                var root = json.RootElement;

                if (!root.TryGetProperty("success", out var successElement)
                    || !successElement.GetBoolean())
                {
                    return OperationResult<RecaptchaV3Verification>.IsFailed(
                        "AUTH_CAPTCHA_04",
                        nameof(VerifyV3Async),
                        $"Google no valido el captcha. {GetErrorCodes(root)}",
                        400);
                }

                if (!root.TryGetProperty("score", out var scoreElement)
                    || !scoreElement.TryGetDouble(out var score))
                {
                    return OperationResult<RecaptchaV3Verification>.IsFailed(
                        "AUTH_CAPTCHA_04",
                        nameof(VerifyV3Async),
                        "Error al obtener el score del captcha.",
                        400);
                }

                var action = root.TryGetProperty("action", out var actionElement)
                    ? actionElement.GetString()
                    : null;

                if (!string.IsNullOrWhiteSpace(expectedAction)
                    && !string.Equals(action, expectedAction, StringComparison.Ordinal))
                {
                    return OperationResult<RecaptchaV3Verification>.IsFailed(
                        "AUTH_CAPTCHA_05",
                        nameof(VerifyV3Async),
                        "La accion del captcha no coincide con la accion esperada.",
                        400);
                }

                return OperationResult<RecaptchaV3Verification>.Ok(
                    new RecaptchaV3Verification(score, action),
                    nameof(VerifyV3Async));
            }
            catch (Exception ex)
            {
                return OperationResult<RecaptchaV3Verification>.IsFailed(
                    "AUTH_CAPTCHA_99",
                    nameof(VerifyV3Async),
                    $"Error al validar captcha: {ex.Message}",
                    500);
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

        private static string MapAuthErrorToRegistrationError(string errorCode) =>
            errorCode switch
            {
                "AUTH_CAPTCHA_02" => "REG_CAPTCHA_02",
                "AUTH_CAPTCHA_03" => "REG_CAPTCHA_03",
                "AUTH_CAPTCHA_04" => "REG_CAPTCHA_04",
                "AUTH_CAPTCHA_05" => "REG_CAPTCHA_04",
                "AUTH_CAPTCHA_99" => "REG_CAPTCHA_99",
                _ => "REG_CAPTCHA_99"
            };

        private static double GetMinimumScore()
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

        private sealed record RecaptchaV3Verification(double Score, string? Action);
    }
}
