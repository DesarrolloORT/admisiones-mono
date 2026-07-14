using AppLogic.Autenticacion.Dtos;
using AppLogic.Autenticacion.Requests;
using AppLogic.Autenticacion.Responses;
using AppLogic.Infrastructure.RateLimiting;
using AppLogic.Autenticacion.Interfaces;
using AppLogic.Autenticacion.Services;
using AppLogic.Common.Validation;
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
                out var authServiceMock,
                out var dosFactoresMock);
            var request = CreateRequest();

            var result = await sut.EjecutarAsync(request, "127.0.0.1", 0.9);

            Assert.False(result.RequiresTwoFactor);
            Assert.True(result.SetCookies);
            Assert.True(result.AuthResult!.Success);
            authServiceMock.Verify(
                s => s.AutenticarUsuarioLDAPAsync("CI", "12345678", "Password1!"),
                Times.Once);
            authServiceMock.Verify(
                s => s.GenerarTokensParaPersonaAsync(123, It.IsAny<string>()),
                Times.Once);
            dosFactoresMock.Verify(
                s => s.IniciarAsync(It.IsAny<DtoPersonaAuth>(), It.IsAny<string>()),
                Times.Never);
        }

        [Theory]
        [InlineData(0.4)]
        [InlineData(0.5)]
        public async Task EjecutarAsync_WithCaptchaScoreAtOrBelowMinimum_StartsTwoFactorFlow(double recaptchaScore)
        {
            var sut = CreateService(
                out _,
                out var dosFactoresMock);
            var request = CreateRequest();

            var result = await sut.EjecutarAsync(request, "127.0.0.1", recaptchaScore);

            Assert.True(result.RequiresTwoFactor);
            Assert.Null(result.AuthResult);
            Assert.True(result.TwoFactorResult!.Success);
            Assert.Equal(202, result.TwoFactorResult.HttpCode);
            Assert.Equal("2fa-session", result.TwoFactorResult.Data!.SessionId);
            dosFactoresMock.Verify(
                s => s.IniciarAsync(
                    It.Is<DtoPersonaAuth>(p => p.Email == "test@example.com"),
                    "test@example.com"),
                Times.Once);
        }

        [Theory]
        [InlineData("1.234.567-8")]
        [InlineData("1234567-8")]
        [InlineData("12345678")]
        [InlineData(" 1.234.567-8 ")]
        public async Task EjecutarAsync_BuildsFailedAttemptKey_UsingDocumentUtilsNormalization(string documento)
        {
            var sut = CreateService(out _, out _, out var rateLimiterMock);
            var request = new DtoAuthRequest
            {
                TipoDocumento = "CI",
                Documento = documento,
                Password = "Password1!"
            };

            await sut.EjecutarAsync(request, "127.0.0.1", 0.9);

            var expectedKey = $"login-fail-cred-user:{DocumentUtils.NormalizarDocumentoParaClave(documento)}";
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
            var sut = CreateService(out _, out _, out var rateLimiterMock);
            var request = new DtoAuthRequest
            {
                TipoDocumento = "CI",
                Documento = documentoConSeparadores,
                Password = "Password1!"
            };

            await sut.EjecutarAsync(request, "127.0.0.1", 0.9);

            // DosFactoresAuthService construye sus keys como $"2fa-init:{DocumentUtils.NormalizarDocumentoParaClave(doc)}".
            // LoginFlowService debe producir el mismo segmento normalizado para el mismo documento.
            var normalizadoEsperado = DocumentUtils.NormalizarDocumentoParaClave(documentoConSeparadores);
            rateLimiterMock.Verify(
                s => s.GetRemainingAsync($"login-fail-cred-user:{normalizadoEsperado}", It.IsAny<int>(), It.IsAny<TimeSpan>()),
                Times.Once);
        }

        private static LoginFlowService CreateService(
            out Mock<IAuthService> authServiceMock,
            out Mock<IDosFactoresAuthService> dosFactoresMock)
        {
            return CreateService(out authServiceMock, out dosFactoresMock, out _);
        }

        private static LoginFlowService CreateService(
            out Mock<IAuthService> authServiceMock,
            out Mock<IDosFactoresAuthService> dosFactoresMock,
            out Mock<IRateLimiterService> rateLimiterMock)
        {
            Environment.SetEnvironmentVariable("RECAPTCHA_SCORE", "0.5");

            authServiceMock = new Mock<IAuthService>();
            authServiceMock
                .Setup(s => s.AutenticarUsuarioLDAPAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(OperationResult<DtoPersonaAuth>.Ok(
                    new DtoPersonaAuth
                    {
                        CodigoPersona = 123,
                        Documento = "12345678",
                        Email = "test@example.com"
                    },
                    nameof(IAuthService.AutenticarUsuarioLDAPAsync)));
            authServiceMock
                .Setup(s => s.GenerarTokensParaPersonaAsync(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync((long codigoPersona, string? _) => OperationResult<DtoAuthenticationResponse>.Ok(
                    new DtoAuthenticationResponse
                    {
                        Persona = new DtoPersonaAuth { CodigoPersona = codigoPersona, Documento = "12345678" },
                        AccessToken = "access-token",
                        RefreshToken = "refresh-token"
                    },
                    nameof(IAuthService.GenerarTokensParaPersonaAsync)));

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

            dosFactoresMock = new Mock<IDosFactoresAuthService>();
            dosFactoresMock
                .Setup(s => s.IniciarAsync(It.IsAny<DtoPersonaAuth>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<DtoLogin2FARequired>.Ok(
                    new DtoLogin2FARequired { SessionId = "2fa-session" },
                    nameof(IDosFactoresAuthService.IniciarAsync)));

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
                dosFactoresMock.Object,
                configuration,
                Mock.Of<ILogger<LoginFlowService>>());
        }

        private static DtoAuthRequest CreateRequest() =>
            new()
            {
                TipoDocumento = "CI",
                Documento = "12345678",
                Password = "Password1!"
            };
    }
}
