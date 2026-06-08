using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Security;
using WebApiAdmisiones.Security.interfaces;

namespace UnitTesting.Security
{
    public class LoginFlowServiceTests
    {
        [Fact]
        public async Task EjecutarAsync_WithCaptchaScoreAboveMinimum_ReturnsSuccessfulLogin()
        {
            var recaptchaScore = 0.9;
            var sut = CreateService(
                recaptchaScore,
                out var authServiceMock,
                out _,
                out var dosFactoresMock);
            var request = CreateRequest();

            var result = await sut.EjecutarAsync(request, "127.0.0.1", "captcha-token");

            Assert.False(result.RequiresTwoFactor);
            Assert.True(result.SetCookies);
            Assert.True(result.AuthResult!.Success);
            authServiceMock.Verify(
                s => s.AutenticarUsuarioLDAPAsync("CI", "12345678", "Password1!"),
                Times.Once);
            dosFactoresMock.Verify(
                s => s.IniciarAsync(It.IsAny<DtoAuthenticationResponse>(), It.IsAny<string>()),
                Times.Never);
        }

        [Theory]
        [InlineData(0.4)]
        [InlineData(0.5)]
        public async Task EjecutarAsync_WithCaptchaScoreAtOrBelowMinimum_StartsTwoFactorFlow(double recaptchaScore)
        {
            var sut = CreateService(
                recaptchaScore,
                out _,
                out var recaptchaMock,
                out var dosFactoresMock);
            var request = CreateRequest();

            var result = await sut.EjecutarAsync(request, "127.0.0.1", "captcha-token");

            Assert.True(result.RequiresTwoFactor);
            Assert.Null(result.AuthResult);
            Assert.True(result.TwoFactorResult!.Success);
            Assert.Equal("2fa-session", result.TwoFactorResult.Data!.SessionId);
            recaptchaMock.Verify(
                s => s.ValidarConScoreAsync("captcha-token", "login"),
                Times.Once);
            dosFactoresMock.Verify(
                s => s.IniciarAsync(
                    It.Is<DtoAuthenticationResponse>(r => r.Persona.Email == "test@example.com"),
                    "test@example.com"),
                Times.Once);
        }

        [Fact]
        public async Task EjecutarAsync_InLocalHost_SkipsCaptchaScoreValidation()
        {
            var sut = CreateService(
                recaptchaScore: 0.4,
                out _,
                out var recaptchaMock,
                out var dosFactoresMock,
                environmentName: "LocalHost");
            var request = CreateRequest();

            var result = await sut.EjecutarAsync(request, "127.0.0.1", "captcha-token");

            Assert.False(result.RequiresTwoFactor);
            Assert.True(result.SetCookies);
            recaptchaMock.Verify(
                s => s.ValidarConScoreAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
            dosFactoresMock.Verify(
                s => s.IniciarAsync(It.IsAny<DtoAuthenticationResponse>(), "test@example.com"),
                Times.Never);
        }

        private static LoginFlowService CreateService(
            double recaptchaScore,
            out Mock<IAuthService> authServiceMock,
            out Mock<IRecaptchaService> recaptchaMock,
            out Mock<IDosFactoresAuthService> dosFactoresMock,
            string environmentName = "Production")
        {
            Environment.SetEnvironmentVariable("RECAPTCHA_SCORE", "0.5");

            authServiceMock = new Mock<IAuthService>();
            authServiceMock
                .Setup(s => s.AutenticarUsuarioLDAPAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(OperationResult<DtoAuthenticationResponse>.Ok(
                    new DtoAuthenticationResponse
                    {
                        Persona = new DtoPersonaAuth
                        {
                            CodigoPersona = 123,
                            Documento = "12345678",
                            Email = "test@example.com"
                        },
                        AccessToken = "access-token",
                        RefreshToken = "refresh-token"
                    },
                    nameof(IAuthService.AutenticarUsuarioLDAPAsync)));

            var rateLimiterMock = new Mock<IRedisRateLimiterService>();
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

            recaptchaMock = new Mock<IRecaptchaService>();
            recaptchaMock
                .Setup(s => s.ValidarConScoreAsync("captcha-token", "login"))
                .ReturnsAsync(OperationResult<double>.Ok(recaptchaScore, nameof(IRecaptchaService.ValidarConScoreAsync)));

            dosFactoresMock = new Mock<IDosFactoresAuthService>();
            dosFactoresMock
                .Setup(s => s.IniciarAsync(It.IsAny<DtoAuthenticationResponse>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<DtoLogin2FARequired>.Ok(
                    new DtoLogin2FARequired { SessionId = "2fa-session" },
                    nameof(IDosFactoresAuthService.IniciarAsync)));

            var environmentMock = new Mock<IWebHostEnvironment>();
            environmentMock
                .Setup(e => e.EnvironmentName)
                .Returns(environmentName);

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
                authServiceMock.Object,
                rateLimiterMock.Object,
                recaptchaMock.Object,
                dosFactoresMock.Object,
                environmentMock.Object,
                configuration,
                Mock.Of<ILogger<LoginFlowService>>());
        }

        private static AuthRequest CreateRequest() =>
            new()
            {
                TipoDocumento = "CI",
                Documento = "12345678",
                Password = "Password1!"
            };
    }
}
