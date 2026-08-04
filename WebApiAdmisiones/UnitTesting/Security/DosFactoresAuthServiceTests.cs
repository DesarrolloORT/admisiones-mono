using AppLogic.Autenticacion.Dtos;
using AppLogic.Common.Email;
using AppLogic.Infrastructure.RateLimiting;
using AppLogic.Autenticacion.Interfaces;
using AppLogic.Autenticacion.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Utilities;

namespace UnitTesting.Security
{
    public class DosFactoresAuthServiceTests
    {
        [Fact]
        public async Task IniciarAsync_WhenMailIsSent_ReturnsMaskedEmailAndUsesRealEmailForDelivery()
        {
            var emailSenderMock = new Mock<IEmailSender>();
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            var service = CreateService(sessionStoreMock: sessionStoreMock, emailSenderMock: emailSenderMock);

            var result = await service.IniciarAsync(CreatePendingPersona(), "gabriele@ort.edu.uy");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("g******e@ort.******", result.Data.MaskedEmail);
            Assert.NotEqual("gabriele@ort.edu.uy", result.Data.MaskedEmail);
            emailSenderMock.Verify(
                e => e.SendAsync("gabriele@ort.edu.uy", It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
            sessionStoreMock.Verify(
                s => s.SaveAsync(It.IsAny<string>(), It.IsAny<DtoTwoFactorSession>(), It.IsAny<TimeSpan>()),
                Times.Once);
        }

        [Fact]
        public async Task IniciarAsync_WhenRateLimitExceeded_Returns429WithoutSavingSession()
        {
            var rateLimiterMock = new Mock<IRateLimiterService>();
            rateLimiterMock
                .Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(false);
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            var service = CreateService(sessionStoreMock: sessionStoreMock, rateLimiterMock: rateLimiterMock);

            var result = await service.IniciarAsync(CreatePendingPersona(), "gabriele@ort.edu.uy");

            Assert.False(result.Success);
            Assert.Equal(429, result.HttpCode);
            Assert.Equal("AUTH_2FA_INIT_01", result.ErrorCode);
            sessionStoreMock.Verify(
                s => s.SaveAsync(It.IsAny<string>(), It.IsAny<DtoTwoFactorSession>(), It.IsAny<TimeSpan>()),
                Times.Never);
        }

        [Fact]
        public async Task IniciarAsync_WhenEmailFails_DeletesSessionAndReturns500()
        {
            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock
                .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP error"));
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            var service = CreateService(sessionStoreMock: sessionStoreMock, emailSenderMock: emailSenderMock);

            var result = await service.IniciarAsync(CreatePendingPersona(), "gabriele@ort.edu.uy");

            Assert.False(result.Success);
            Assert.Equal(500, result.HttpCode);
            Assert.Equal("AUTH_2FA_MAIL_01", result.ErrorCode);
            sessionStoreMock.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task VerificarCodigoAsync_WhenSessionNotFound_Returns401()
        {
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            sessionStoreMock.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((DtoTwoFactorSession?)null);
            var service = CreateService(sessionStoreMock: sessionStoreMock);

            var result = await service.VerificarCodigoAsync("missing-session", "123456");

            Assert.False(result.Success);
            Assert.Equal(401, result.HttpCode);
            Assert.Equal("AUTH_2FA_02", result.ErrorCode);
        }

        [Fact]
        public async Task VerificarCodigoAsync_WhenCodeExpired_ReturnsUnauthorizedWithoutDeletingSession()
        {
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            sessionStoreMock
                .Setup(s => s.GetAsync("session-id"))
                .ReturnsAsync(new DtoTwoFactorSession
                {
                    CodigoHash = "any-hash",
                    CodigoExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
                    Documento = "12345678"
                });
            var service = CreateService(sessionStoreMock: sessionStoreMock);

            var result = await service.VerificarCodigoAsync("session-id", "123456");

            Assert.False(result.Success);
            Assert.Equal(401, result.HttpCode);
            Assert.Equal("AUTH_2FA_06", result.ErrorCode);
            sessionStoreMock.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerificarCodigoAsync_WhenCodeIsCorrect_ReturnsAuthResponse()
        {
            var codigo = "123456";
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigo))).ToLowerInvariant();
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            sessionStoreMock
                .Setup(s => s.GetAsync("session-id"))
                .ReturnsAsync(new DtoTwoFactorSession
                {
                    CodigoPersona = 123,
                    CodigoHash = hash,
                    CodigoExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                    Documento = "1.234.567-8"
                });
            var authServiceMock = new Mock<IAuthService>();
            authServiceMock
                .Setup(a => a.GenerarTokensParaPersonaAsync(123, It.IsAny<string>()))
                .ReturnsAsync(OperationResult<DtoAuthenticationResponse>.Ok(
                    new DtoAuthenticationResponse
                    {
                        Persona = new DtoPersonaAuth { CodigoPersona = 123 },
                        AccessToken = "access-token",
                        RefreshToken = "refresh-token",
                        RefreshTokenHash = "refresh-token-hash"
                    },
                    nameof(IAuthService.GenerarTokensParaPersonaAsync)));
            var service = CreateService(sessionStoreMock: sessionStoreMock, authServiceMock: authServiceMock);

            var result = await service.VerificarCodigoAsync("session-id", codigo);

            Assert.True(result.Success);
            Assert.Equal(123, result.Data!.Persona.CodigoPersona);
            Assert.Equal("access-token", result.Data.AccessToken);
            sessionStoreMock.Verify(s => s.DeleteAsync("session-id"), Times.Once);
            authServiceMock.Verify(a => a.GenerarTokensParaPersonaAsync(123, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task VerificarCodigoAsync_WhenTokenGenerationFails_ReturnsFailureWithoutLeakingTokens()
        {
            // SEG-03: si emitir los tokens falla después de un código correcto, la falla se
            // propaga tal cual (mismo ErrorCode/HttpCode que GenerarTokensParaPersonaAsync).
            var codigo = "123456";
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigo))).ToLowerInvariant();
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            sessionStoreMock
                .Setup(s => s.GetAsync("session-id"))
                .ReturnsAsync(new DtoTwoFactorSession
                {
                    CodigoPersona = 123,
                    CodigoHash = hash,
                    CodigoExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                    Documento = "1.234.567-8"
                });
            var authServiceMock = new Mock<IAuthService>();
            authServiceMock
                .Setup(a => a.GenerarTokensParaPersonaAsync(123, It.IsAny<string>()))
                .ReturnsAsync(OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "GEN_TOK_01",
                    nameof(IAuthService.GenerarTokensParaPersonaAsync),
                    "Usuario no encontrado en la base de datos.",
                    404,
                    default!));
            var service = CreateService(sessionStoreMock: sessionStoreMock, authServiceMock: authServiceMock);

            var result = await service.VerificarCodigoAsync("session-id", codigo);

            Assert.False(result.Success);
            Assert.Equal("GEN_TOK_01", result.ErrorCode);
            Assert.Equal(404, result.HttpCode);
            sessionStoreMock.Verify(s => s.DeleteAsync("session-id"), Times.Once);
        }

        [Fact]
        public async Task VerificarCodigoAsync_WhenCodeIsCorrect_ClearsTwoFactorInitRateLimit()
        {
            var codigo = "123456";
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigo))).ToLowerInvariant();
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            sessionStoreMock
                .Setup(s => s.GetAsync("session-id"))
                .ReturnsAsync(new DtoTwoFactorSession
                {
                    CodigoHash = hash,
                    CodigoExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                    Documento = "1.234.567-8"
                });
            var rateLimiterMock = new Mock<IRateLimiterService>();
            rateLimiterMock.Setup(r => r.ClearAsync(It.IsAny<string>())).ReturnsAsync(true);
            var service = CreateService(sessionStoreMock: sessionStoreMock, rateLimiterMock: rateLimiterMock);

            var result = await service.VerificarCodigoAsync("session-id", codigo);

            Assert.True(result.Success);
            rateLimiterMock.Verify(r => r.ClearAsync("2fa-init:12345678"), Times.Once);
        }

        [Fact]
        public async Task VerificarCodigoAsync_WhenCodeIsIncorrect_IncrementsAttemptsAndUpdatesSession()
        {
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            sessionStoreMock
                .Setup(s => s.GetAsync("session-id"))
                .ReturnsAsync(new DtoTwoFactorSession
                {
                    CodigoHash = "correct-hash",
                    CodigoExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                    Documento = "12345678",
                    Intentos = 0
                });
            sessionStoreMock
                .Setup(s => s.GetTtlAsync("session-id"))
                .ReturnsAsync(TimeSpan.FromMinutes(25));
            DtoTwoFactorSession? capturedSession = null;
            sessionStoreMock
                .Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<DtoTwoFactorSession>(), It.IsAny<TimeSpan>()))
                .Callback<string, DtoTwoFactorSession, TimeSpan>((_, session, _) => capturedSession = session)
                .Returns(Task.CompletedTask);
            var service = CreateService(sessionStoreMock: sessionStoreMock);

            var result = await service.VerificarCodigoAsync("session-id", "wrong-code");

            Assert.False(result.Success);
            Assert.Equal(401, result.HttpCode);
            Assert.Equal("AUTH_2FA_05", result.ErrorCode);
            Assert.NotNull(capturedSession);
            Assert.Equal(1, capturedSession.Intentos);
        }

        [Fact]
        public async Task VerificarCodigoAsync_WhenMaxAttemptsExceeded_DeletesSession()
        {
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            sessionStoreMock
                .Setup(s => s.GetAsync("session-id"))
                .ReturnsAsync(new DtoTwoFactorSession
                {
                    CodigoHash = "correct-hash",
                    CodigoExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                    Documento = "12345678",
                    Intentos = 4
                });
            var service = CreateService(sessionStoreMock: sessionStoreMock);

            var result = await service.VerificarCodigoAsync("session-id", "wrong-code");

            Assert.False(result.Success);
            Assert.Equal(401, result.HttpCode);
            Assert.Equal("AUTH_2FA_04", result.ErrorCode);
            sessionStoreMock.Verify(s => s.DeleteAsync("session-id"), Times.Once);
        }

        [Fact]
        public async Task ReenviarCodigoAsync_WhenSessionNotFound_Returns401()
        {
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            sessionStoreMock.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((DtoTwoFactorSession?)null);
            var service = CreateService(sessionStoreMock: sessionStoreMock);

            var result = await service.ReenviarCodigoAsync("missing-session");

            Assert.False(result.Success);
            Assert.Equal(401, result.HttpCode);
            Assert.Equal("AUTH_2FA_02", result.ErrorCode);
        }

        [Fact]
        public async Task ReenviarCodigoAsync_WhenSessionExists_SendsNewCodeResetsAttemptsAndPreservesTtl()
        {
            var originalHash = "original-hash";
            var remainingTtl = TimeSpan.FromMinutes(17);
            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            sessionStoreMock
                .Setup(s => s.GetAsync("session-id"))
                .ReturnsAsync(new DtoTwoFactorSession
                {
                    CodigoHash = originalHash,
                    CodigoExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
                    Email = "gabriele@ort.edu.uy",
                    Documento = "1.234.567-8",
                    Intentos = 3
                });
            sessionStoreMock
                .Setup(s => s.GetTtlAsync("session-id"))
                .ReturnsAsync(remainingTtl);
            DtoTwoFactorSession? capturedSession = null;
            TimeSpan capturedTtl = default;
            sessionStoreMock
                .Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<DtoTwoFactorSession>(), It.IsAny<TimeSpan>()))
                .Callback<string, DtoTwoFactorSession, TimeSpan>((_, session, ttl) =>
                {
                    capturedSession = session;
                    capturedTtl = ttl;
                })
                .Returns(Task.CompletedTask);
            var emailSenderMock = new Mock<IEmailSender>();
            var service = CreateService(sessionStoreMock: sessionStoreMock, emailSenderMock: emailSenderMock);

            var result = await service.ReenviarCodigoAsync("session-id");

            Assert.True(result.Success);
            Assert.Equal("g******e@ort.******", result.Data!.MaskedEmail);
            emailSenderMock.Verify(
                e => e.SendAsync("gabriele@ort.edu.uy", It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
            Assert.NotNull(capturedSession);
            Assert.NotEqual(originalHash, capturedSession.CodigoHash);
            Assert.Equal(0, capturedSession.Intentos);
            Assert.True(capturedSession.CodigoExpiresAtUtc > DateTime.UtcNow);
            Assert.Equal(remainingTtl, capturedTtl);
        }

        private static DtoPersonaAuth CreatePendingPersona() =>
            new()
            {
                CodigoPersona = 123,
                Documento = "1.234.567-8",
                PrimerNombre = "Gabriele",
                PrimerApellido = "Test"
            };

        private static IConfiguration CreateConfiguration() =>
            new ConfigurationBuilder()
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

        private static DosFactoresAuthService CreateService(
            Mock<ITwoFactorSessionStore>? sessionStoreMock = null,
            Mock<IEmailSender>? emailSenderMock = null,
            Mock<IRateLimiterService>? rateLimiterMock = null,
            Mock<IAuthService>? authServiceMock = null)
        {
            if (sessionStoreMock == null)
            {
                sessionStoreMock = new Mock<ITwoFactorSessionStore>();
                sessionStoreMock
                    .Setup(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<DtoTwoFactorSession>(), It.IsAny<TimeSpan>()))
                    .Returns(Task.CompletedTask);
                sessionStoreMock
                    .Setup(s => s.DeleteAsync(It.IsAny<string>()))
                    .Returns(Task.CompletedTask);
                sessionStoreMock
                    .Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<DtoTwoFactorSession>(), It.IsAny<TimeSpan>()))
                    .Returns(Task.CompletedTask);
            }

            if (emailSenderMock == null)
            {
                emailSenderMock = new Mock<IEmailSender>();
                emailSenderMock
                    .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);
            }

            if (rateLimiterMock == null)
            {
                rateLimiterMock = new Mock<IRateLimiterService>();
                rateLimiterMock
                    .Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                    .ReturnsAsync(true);
                rateLimiterMock
                    .Setup(r => r.ClearAsync(It.IsAny<string>()))
                    .ReturnsAsync(true);
            }

            if (authServiceMock == null)
            {
                authServiceMock = new Mock<IAuthService>();
                authServiceMock
                    .Setup(a => a.GenerarTokensParaPersonaAsync(It.IsAny<long>(), It.IsAny<string>()))
                    .ReturnsAsync((long codigoPersona, string? _) => OperationResult<DtoAuthenticationResponse>.Ok(
                        new DtoAuthenticationResponse
                        {
                            Persona = new DtoPersonaAuth { CodigoPersona = codigoPersona },
                            AccessToken = "default-access-token",
                            RefreshToken = "default-refresh-token",
                            RefreshTokenHash = "default-refresh-token-hash"
                        },
                        nameof(IAuthService.GenerarTokensParaPersonaAsync)));
            }

            return new DosFactoresAuthService(
                sessionStoreMock.Object,
                rateLimiterMock.Object,
                emailSenderMock.Object,
                CreateConfiguration(),
                Mock.Of<ILogger<DosFactoresAuthService>>(),
                authServiceMock.Object);
        }
    }
}
