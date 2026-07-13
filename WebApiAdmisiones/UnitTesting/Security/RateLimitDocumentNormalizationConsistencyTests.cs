using AppLogic.Autenticacion.Dtos;
using AppLogic.Autenticacion.Requests;
using AppLogic.Autenticacion.Responses;
using AppLogic.Common.Email;
using AppLogic.Infrastructure.RateLimiting;
using AppLogic.Autenticacion.Interfaces;
using AppLogic.Autenticacion.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;

namespace UnitTesting.Security
{
    /// <summary>
    /// Ambos servicios delegan en DocumentUtils.NormalizarDocumentoParaClave para construir
    /// sus claves de rate limiting; este test cruza los dos flujos reales (sin mocks de
    /// DocumentUtils) para confirmar que el segmento normalizado coincide para el mismo documento.
    /// </summary>
    public class RateLimitDocumentNormalizationConsistencyTests
    {
        [Theory]
        [InlineData("1.234.567-8")]
        [InlineData("1234567-8")]
        [InlineData("12345678")]
        [InlineData(" 1.234.567-8 ")]
        public async Task LoginFlowService_And_DosFactoresAuthService_NormalizeDocumentIdentically(string documento)
        {
            var loginFlowKey = await CaptureLoginFlowFailedAttemptKeyAsync(documento);
            var dosFactoresKey = await CaptureDosFactoresInitKeyAsync(documento);

            var loginFlowSegment = loginFlowKey["login-fail-cred-user:".Length..];
            var dosFactoresSegment = dosFactoresKey["2fa-init:".Length..];

            Assert.Equal(dosFactoresSegment, loginFlowSegment);
        }

        private static async Task<string> CaptureLoginFlowFailedAttemptKeyAsync(string documento)
        {
            string? capturedKey = null;

            var authServiceMock = new Mock<IAuthService>();
            authServiceMock
                .Setup(s => s.AutenticarUsuarioLDAPAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<DtoAuthenticationResponse>.Ok(
                    new DtoAuthenticationResponse
                    {
                        Persona = new DtoPersonaAuth
                        {
                            CodigoPersona = 123,
                            Documento = documento,
                            Email = "test@example.com"
                        },
                        AccessToken = "access-token",
                        RefreshToken = "refresh-token"
                    },
                    nameof(IAuthService.AutenticarUsuarioLDAPAsync)));

            var rateLimiterMock = new Mock<IRateLimiterService>();
            rateLimiterMock
                .Setup(r => r.ValidateAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(new RateLimitValidationResult
                {
                    IsAllowed = true,
                    RemainingAttempts = 4,
                    PartitionKey = "login"
                });
            rateLimiterMock
                .Setup(r => r.GetRemainingAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .Callback<string, int, TimeSpan>((key, _, _) => capturedKey ??= key)
                .ReturnsAsync(1);
            rateLimiterMock
                .Setup(r => r.ClearAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            var dosFactoresMock = new Mock<IDosFactoresAuthService>();
            dosFactoresMock
                .Setup(s => s.IniciarAsync(It.IsAny<DtoAuthenticationResponse>(), It.IsAny<string>()))
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

            var sut = new LoginFlowService(
                authServiceMock.Object,
                rateLimiterMock.Object,
                dosFactoresMock.Object,
                configuration,
                Mock.Of<ILogger<LoginFlowService>>());

            await sut.EjecutarAsync(
                new DtoAuthRequest { TipoDocumento = "CI", Documento = documento, Password = "Password1!" },
                "127.0.0.1",
                0.9);

            Assert.NotNull(capturedKey);
            return capturedKey!;
        }

        private static async Task<string> CaptureDosFactoresInitKeyAsync(string documento)
        {
            string? capturedKey = null;

            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            sessionStoreMock
                .Setup(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<DtoTwoFactorSession>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock
                .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var rateLimiterMock = new Mock<IRateLimiterService>();
            rateLimiterMock
                .Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .Callback<string, int, TimeSpan>((key, _, _) => capturedKey ??= key)
                .ReturnsAsync(true);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:TwoFactor:MaxInitAttempts"] = "3",
                    ["Authentication:TwoFactor:SessionMinutes"] = "30",
                    ["Authentication:TwoFactor:CodeMinutes"] = "10",
                    ["Authentication:TwoFactor:CodeLength"] = "6",
                    ["Authentication:TwoFactor:MaxCodeAttempts"] = "5",
                    ["Mail:From"] = "admisiones@example.com"
                })
                .Build();

            var sut = new DosFactoresAuthService(
                sessionStoreMock.Object,
                rateLimiterMock.Object,
                emailSenderMock.Object,
                configuration,
                Mock.Of<ILogger<DosFactoresAuthService>>());

            var pendingAuth = new DtoAuthenticationResponse
            {
                Persona = new DtoPersonaAuth
                {
                    CodigoPersona = 123,
                    Documento = documento,
                    PrimerNombre = "Gabriele",
                    PrimerApellido = "Test"
                },
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                RefreshTokenHash = "refresh-token-hash"
            };

            await sut.IniciarAsync(pendingAuth, "gabriele@ort.edu.uy");

            Assert.NotNull(capturedKey);
            return capturedKey!;
        }
    }
}
