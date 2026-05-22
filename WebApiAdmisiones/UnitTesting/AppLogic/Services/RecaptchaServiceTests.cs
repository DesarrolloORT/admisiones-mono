using System.Net;
using System.Text;
using AppLogic.Services;

namespace UnitTesting.AppLogic.Services
{
    [Collection(EnvironmentVariablesCollection.Name)]
    public class RecaptchaServiceTests
    {
        [Fact]
        public async Task ValidarAsync_WithEmptyToken_ReturnsBadRequestAndDoesNotCallGoogle()
        {
            var handler = new StubHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK));
            var service = new RecaptchaService(new HttpClient(handler));

            var result = await service.ValidarAsync(" ");

            Assert.False(result.Success);
            Assert.Equal("REG_CAPTCHA_01", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task ValidarAsync_WithoutConfiguration_ReturnsServerError()
        {
            using var scope = new EnvironmentVariableScope(
                ("RECAPTCHA_SITE_KEY", null),
                ("RECAPTCHA_API_KEY", null));
            var service = new RecaptchaService(new HttpClient(new StubHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK))));

            var result = await service.ValidarAsync("captcha-token");

            Assert.False(result.Success);
            Assert.Equal("REG_CAPTCHA_02", result.ErrorCode);
            Assert.Equal(500, result.HttpCode);
        }

        [Fact]
        public async Task ValidarAsync_WithAcceptedScore_ReturnsOk()
        {
            using var scope = new EnvironmentVariableScope(
                ("RECAPTCHA_SITE_KEY", "site-key"),
                ("RECAPTCHA_API_KEY", "api-key"),
                ("RECAPTCHA_SCORE", "0.7"));
            var handler = new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """{"riskAnalysis":{"score":0.9}}"""));
            var service = new RecaptchaService(new HttpClient(handler));

            var result = await service.ValidarAsync("captcha-token");

            Assert.True(result.Success);
            Assert.True(result.Data);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("projects/admisiones-457619/assessments?key=api-key", request.RequestUri);
            Assert.Contains("captcha-token", request.Body);
            Assert.Contains("site-key", request.Body);
        }

        [Fact]
        public async Task ValidarAsync_WithLowScore_ReturnsSuspiciousActivity()
        {
            using var scope = new EnvironmentVariableScope(
                ("RECAPTCHA_SITE_KEY", "site-key"),
                ("RECAPTCHA_API_KEY", "api-key"),
                ("RECAPTCHA_SCORE", "0.8"));
            var service = new RecaptchaService(new HttpClient(new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.OK, """{"riskAnalysis":{"score":0.3}}"""))));

            var result = await service.ValidarAsync("captcha-token");

            Assert.False(result.Success);
            Assert.Equal("REG_CAPTCHA_05", result.ErrorCode);
            Assert.Equal(400, result.HttpCode);
        }

        [Fact]
        public async Task ValidarAsync_WhenGoogleReturnsError_ReturnsBadGateway()
        {
            using var scope = new EnvironmentVariableScope(
                ("RECAPTCHA_SITE_KEY", "site-key"),
                ("RECAPTCHA_API_KEY", "api-key"));
            var service = new RecaptchaService(new HttpClient(new StubHttpMessageHandler(_ =>
                JsonResponse(HttpStatusCode.BadRequest, """{"error":"invalid"}"""))));

            var result = await service.ValidarAsync("captcha-token");

            Assert.False(result.Success);
            Assert.Equal("REG_CAPTCHA_03", result.ErrorCode);
            Assert.Equal(502, result.HttpCode);
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
