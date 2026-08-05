using AppLogic.Authentication.Contracts;
using AppLogic.Identity.Dtos;
using AppLogic.Contracts.Text;
using AppLogic.Authentication.Dtos;
using AppLogic.Platform.RateLimiting;
using AppLogic.Authentication.Interfaces;
using AppLogic.Authentication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;

namespace UnitTesting.Security
{
    public class LoginFlowServiceTests
    {
        [Fact]
        public async Task EjecutarAsync_WithCaptchaScoreAboveMinimum_ReturnsSuccessfulLogin()
        {
            var sut = CreateService(
                out var authenticateMock,
                out var issueTokensMock,
                out var dosFactoresMock);
            var request = CreateRequest();

            var result = await sut.ExecuteAsync(request, "127.0.0.1", 0.9);

            Assert.False(result.RequiresTwoFactor);
            Assert.True(result.SetCookies);
            Assert.True(result.AuthResult!.Success);
            authenticateMock.Verify(
                s => s.ExecuteAsync("CI", "12345678", "Password1!"),
                Times.Once);
            issueTokensMock.Verify(
                s => s.ExecuteAsync(123, It.IsAny<string>()),
                Times.Once);
            dosFactoresMock.Verify(
                s => s.StartAsync(It.IsAny<AuthenticatedPerson>(), It.IsAny<string>()),
                Times.Never);
        }

        [Theory]
        [InlineData(0.4)]
        [InlineData(0.5)]
        public async Task EjecutarAsync_WithCaptchaScoreAtOrBelowMinimum_StartsTwoFactorFlow(double recaptchaScore)
        {
            var sut = CreateService(
                out _,
                out _,
                out var dosFactoresMock);
            var request = CreateRequest();

            var result = await sut.ExecuteAsync(request, "127.0.0.1", recaptchaScore);

            Assert.True(result.RequiresTwoFactor);
            Assert.Null(result.AuthResult);
            Assert.True(result.TwoFactorResult!.Success);
            Assert.Equal(202, result.TwoFactorResult.HttpCode);
            Assert.Equal("2fa-session", result.TwoFactorResult.Data!.SessionId);
            dosFactoresMock.Verify(
                s => s.StartAsync(
                    It.Is<AuthenticatedPerson>(p => p.Email == "test@example.com"),
                    "test@example.com"),
                Times.Once);
        }

        [Theory]
        [InlineData("1.234.567-8")]
        [InlineData("1234567-8")]
        [InlineData("12345678")]
        [InlineData(" 1.234.567-8 ")]
        public async Task EjecutarAsync_BuildsFailedAttemptKey_UsingDocumentUtilsNormalization(string document)
        {
            var sut = CreateService(out _, out _, out _, out var rateLimiterMock);
            var request = new AuthRequest
            {
                DocumentType = "CI",
                DocumentNumber = document,
                Password = "Password1!"
            };

            await sut.ExecuteAsync(request, "127.0.0.1", 0.9);

            var expectedKey = $"login-fail-cred-user:{TextNormalization.NormalizeDocumentForKey(document)}";
            rateLimiterMock.Verify(
                s => s.GetRemainingAsync(expectedKey, It.IsAny<int>(), It.IsAny<TimeSpan>()),
                Times.Once);
            rateLimiterMock.Verify(
                s => s.ClearAsync(expectedKey),
                Times.Once);
        }

        [Fact]
        public async Task EjecutarAsync_NormalizesDocumentSameWayAsDosFactoresAuthService()
        {
            const string documentoConSeparadores = "1.234.567-8";
            var sut = CreateService(out _, out _, out _, out var rateLimiterMock);
            var request = new AuthRequest
            {
                DocumentType = "CI",
                DocumentNumber = documentoConSeparadores,
                Password = "Password1!"
            };

            await sut.ExecuteAsync(request, "127.0.0.1", 0.9);

            // TwoFactorAuthService construye sus keys como $"2fa-init:{TextNormalization.NormalizeDocumentForKey(doc)}".
            // LoginFlowService debe producir el mismo segmento normalizado para el mismo documento.
            var normalizadoEsperado = TextNormalization.NormalizeDocumentForKey(documentoConSeparadores);
            rateLimiterMock.Verify(
                s => s.GetRemainingAsync($"login-fail-cred-user:{normalizadoEsperado}", It.IsAny<int>(), It.IsAny<TimeSpan>()),
                Times.Once);
        }

        private static LoginFlowService CreateService(
            out Mock<IAuthenticateWithLdap> authenticateMock,
            out Mock<IIssueTokensForPerson> issueTokensMock,
            out Mock<ITwoFactorAuthService> dosFactoresMock)
        {
            return CreateService(out authenticateMock, out issueTokensMock, out dosFactoresMock, out _);
        }

        private static LoginFlowService CreateService(
            out Mock<IAuthenticateWithLdap> authenticateMock,
            out Mock<IIssueTokensForPerson> issueTokensMock,
            out Mock<ITwoFactorAuthService> dosFactoresMock,
            out Mock<IRateLimiterService> rateLimiterMock)
        {
            Environment.SetEnvironmentVariable("RECAPTCHA_SCORE", "0.5");

            authenticateMock = new Mock<IAuthenticateWithLdap>();
            authenticateMock
                .Setup(s => s.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(OperationResult<AuthenticatedPerson>.Ok(
                    new AuthenticatedPerson
                    {
                        PersonId = 123,
                        DocumentNumber = "12345678",
                        Email = "test@example.com"
                    },
                    nameof(IAuthenticateWithLdap)));
            issueTokensMock = new Mock<IIssueTokensForPerson>();
            issueTokensMock
                .Setup(s => s.ExecuteAsync(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync((long personId, string? _) => OperationResult<AuthenticationResponse>.Ok(
                    new AuthenticationResponse
                    {
                        Person = new AuthenticatedPerson { PersonId = personId, DocumentNumber = "12345678" },
                        AccessToken = "access-token",
                        RefreshToken = "refresh-token"
                    },
                    nameof(IIssueTokensForPerson)));

            rateLimiterMock = new Mock<IRateLimiterService>();
            rateLimiterMock
                .Setup(s => s.ValidateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new RateLimitValidationResult
                {
                    IsAllowed = true,
                    RemainingAttempts = 4,
                    PartitionKey = "login:127.0.0.1:12345678"
                });
            rateLimiterMock
                .Setup(s => s.GetRemainingAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(1);
            rateLimiterMock
                .Setup(s => s.ClearAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            dosFactoresMock = new Mock<ITwoFactorAuthService>();
            dosFactoresMock
                .Setup(s => s.StartAsync(It.IsAny<AuthenticatedPerson>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<TwoFactorRequiredResponse>.Ok(
                    new TwoFactorRequiredResponse { SessionId = "2fa-session" },
                    nameof(ITwoFactorAuthService.StartAsync)));

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Login:RateLimitAccountAttempts"] = "5",
                    ["Authentication:Login:RateLimitWindowMinutes"] = "15",
                    ["Authentication:Login:FailedAttemptLimitUser"] = "5",
                    ["Authentication:Login:FailedAttemptLimitIp"] = "10",
                    ["Authentication:Login:FailedAttemptWindowMinutes"] = "15"
                })
                .Build();

            return new LoginFlowService(
                authenticateMock.Object,
                issueTokensMock.Object,
                rateLimiterMock.Object,
                dosFactoresMock.Object,
                configuration,
                Mock.Of<ILogger<LoginFlowService>>());
        }

        private static AuthRequest CreateRequest() =>
            new()
            {
                DocumentType = "CI",
                DocumentNumber = "12345678",
                Password = "Password1!"
            };
    }
}
