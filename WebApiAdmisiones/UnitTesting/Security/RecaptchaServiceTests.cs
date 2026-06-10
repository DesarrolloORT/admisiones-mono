using System.Net;
using System.Text;
using UnitTesting.AppLogic.Services;
using WebApiAdmisiones.Security.Captcha;

namespace UnitTesting.Security
{
    [Collection(EnvironmentVariablesCollection.Name)]
    public class RecaptchaServiceTests
    {
        [Fact]
        public async Task ValidarConScoreAsync_WithEmptyToken_ReturnsBadRequestAndDoesNotCallGoogle()
        {
            var handler = new StubHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK));
            var service = new RecaptchaService(new HttpClient(handler));

            var result = await service.ValidarConScoreAsync(" ");

            Assert.False(result.Success);
            Assert.Equal("AUTH_CAPTCHA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task ValidarConScoreAsync_WithoutConfiguration_ReturnsServerError()
        {
            using var scope = new EnvironmentVariableScope(
                ("RECAPTCHA_SECRET_KEY", null));
            var service = new RecaptchaService(new HttpClient(new StubHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK))));

            var result = await service.ValidarConScoreAsync("captcha-token");

            Assert.False(result.Success);
            Assert.Equal("AUTH_CAPTCHA_02", result.ErrorCode);
            Assert.Equal(500, result.HttpCode);
        }

        [Fact]
        public async Task ValidarConScoreAsync_WithValidV3Response_ReturnsScore()
        {
            using var scope = new EnvironmentVariableScope(
                ("RECAPTCHA_SECRET_KEY", "secret-key"));
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """{"success":true,"score":0.9,"action":"login"}"""));
            var service = new RecaptchaService(new HttpClient(handler));

            var result = await service.ValidarConScoreAsync("captcha-token", "login");

            Assert.True(result.Success);
            Assert.Equal(0.9, result.Data);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://www.google.com/recaptcha/api/siteverify", request.RequestUri);
            Assert.Contains("secret=secret-key", request.Body);
            Assert.Contains("response=captcha-token", request.Body);
        }

        [Fact]
        public async Task ValidarConScoreAsync_WhenGoogleReturnsError_ReturnsBadGateway()
        {
            using var scope = new EnvironmentVariableScope(
                ("RECAPTCHA_SECRET_KEY", "secret-key"));
            var service = new RecaptchaService(new HttpClient(new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.BadRequest, """{"error":"invalid"}"""))));

            var result = await service.ValidarConScoreAsync("captcha-token");

            Assert.False(result.Success);
            Assert.Equal("AUTH_CAPTCHA_03", result.ErrorCode);
            Assert.Equal(502, result.HttpCode);
        }

        [Fact]
        public async Task ValidarConScoreAsync_WhenActionDoesNotMatch_ReturnsBadRequest()
        {
            using var scope = new EnvironmentVariableScope(
                ("RECAPTCHA_SECRET_KEY", "secret-key"));
            var service = new RecaptchaService(new HttpClient(new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """{"success":true,"score":0.9,"action":"registro"}"""))));

            var result = await service.ValidarConScoreAsync("captcha-token", "login");

            Assert.False(result.Success);
            Assert.Equal("AUTH_CAPTCHA_05", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string body)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : HttpMessageHandler
        {
            public List<CapturedRequest> Requests { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(new CapturedRequest(
                    request.Method,
                    request.RequestUri?.ToString() ?? string.Empty,
                    request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));

                return handler(request);
            }
        }

        private sealed record CapturedRequest(HttpMethod Method, string RequestUri, string Body);
    }
}
