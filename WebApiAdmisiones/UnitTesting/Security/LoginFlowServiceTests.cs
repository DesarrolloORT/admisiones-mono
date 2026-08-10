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

        [Fact]
        public async Task EjecutarAsync_CuentaConRateLimitSuperado_Devuelve429ConHeaders()
        {
            var resetTime = DateTimeOffset.UtcNow.AddMinutes(15);
            var sut = CreateService(out var authenticateMock, out _, out _, out var rateLimiterMock);
            rateLimiterMock
                .Setup(s => s.ValidateAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(new RateLimitValidationResult
                {
                    IsAllowed = false,
                    RemainingAttempts = 0,
                    ResetTime = resetTime,
                    PartitionKey = "login:127.0.0.1:12345678"
                });

            var result = await sut.ExecuteAsync(CreateRequest(), "127.0.0.1", 0.9);

            Assert.False(result.AuthResult!.Success);
            Assert.Equal("AUTH_RL_02", result.AuthResult.ErrorCode);
            Assert.Equal(429, result.AuthResult.HttpCode);
            Assert.NotNull(result.RateLimitHeaders);
            Assert.Equal(5, result.RateLimitHeaders.Limit);
            Assert.Equal(0, result.RateLimitHeaders.Remaining);
            Assert.Equal(resetTime, result.RateLimitHeaders.ResetTime);
            authenticateMock.Verify(
                s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EjecutarAsync_BloqueoPorFallidosDelUsuario_Devuelve429SinConsultarLdap()
        {
            var sut = CreateService(out var authenticateMock, out _, out _, out var rateLimiterMock);
            rateLimiterMock
                .Setup(s => s.GetRemainingAsync(
                    It.Is<string>(k => k.StartsWith("login-fail-cred-user:")), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(0);

            var result = await sut.ExecuteAsync(CreateRequest(), "127.0.0.1", 0.9);

            Assert.False(result.AuthResult!.Success);
            Assert.Equal("AUTH_RL_03", result.AuthResult.ErrorCode);
            Assert.Equal(429, result.AuthResult.HttpCode);
            Assert.Contains("15 minutos", result.AuthResult.Message);
            authenticateMock.Verify(
                s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EjecutarAsync_BloqueoPorFallidosDeLaIp_Devuelve429SinConsultarLdap()
        {
            var sut = CreateService(out var authenticateMock, out _, out _, out var rateLimiterMock);
            rateLimiterMock
                .Setup(s => s.GetRemainingAsync(
                    It.Is<string>(k => k.StartsWith("login-fail-cred-ip:")), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(0);

            var result = await sut.ExecuteAsync(CreateRequest(), "127.0.0.1", 0.9);

            Assert.False(result.AuthResult!.Success);
            Assert.Equal("AUTH_RL_04", result.AuthResult.ErrorCode);
            Assert.Equal(429, result.AuthResult.HttpCode);
            authenticateMock.Verify(
                s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EjecutarAsync_CredencialesInvalidas_CuentaElFallidoEnUsuarioYIp()
        {
            var sut = CreateService(out var authenticateMock, out var issueTokensMock, out _, out var rateLimiterMock);
            authenticateMock
                .Setup(s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<AuthenticatedPerson>.IsFailed(
                    "AUTH_01", nameof(IAuthenticateWithLdap), "Credenciales inválidas.", 401, default!));

            var result = await sut.ExecuteAsync(CreateRequest(), "127.0.0.1", 0.9);

            Assert.False(result.AuthResult!.Success);
            Assert.Equal("AUTH_01", result.AuthResult.ErrorCode);
            rateLimiterMock.Verify(
                s => s.IsAllowedAsync(
                    It.Is<string>(k => k.StartsWith("login-fail-cred-user:")), It.IsAny<int>(), It.IsAny<TimeSpan>()),
                Times.Once);
            rateLimiterMock.Verify(
                s => s.IsAllowedAsync(
                    It.Is<string>(k => k.StartsWith("login-fail-cred-ip:")), It.IsAny<int>(), It.IsAny<TimeSpan>()),
                Times.Once);
            rateLimiterMock.Verify(s => s.ClearAsync(It.IsAny<string>()), Times.Never);
            issueTokensMock.Verify(s => s.ExecuteAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EjecutarAsync_LdapDevuelveExitoSinPersona_TrataComoFallo()
        {
            var sut = CreateService(out var authenticateMock, out var issueTokensMock, out _, out _);
            authenticateMock
                .Setup(s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<AuthenticatedPerson>.Ok(null!, nameof(IAuthenticateWithLdap)));

            var result = await sut.ExecuteAsync(CreateRequest(), "127.0.0.1", 0.9);

            Assert.False(result.AuthResult!.Success);
            issueTokensMock.Verify(s => s.ExecuteAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task EjecutarAsync_ScoreBajoSinEmail_Devuelve422SinIniciar2Fa(string? email)
        {
            var sut = CreateService(out var authenticateMock, out _, out var dosFactoresMock, out _);
            authenticateMock
                .Setup(s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<AuthenticatedPerson>.Ok(
                    new AuthenticatedPerson { PersonId = 123, DocumentNumber = "12345678", Email = email },
                    nameof(IAuthenticateWithLdap)));

            var result = await sut.ExecuteAsync(CreateRequest(), "127.0.0.1", 0.4);

            Assert.False(result.RequiresTwoFactor);
            Assert.False(result.AuthResult!.Success);
            Assert.Equal("AUTH_2FA_NO_EMAIL", result.AuthResult.ErrorCode);
            Assert.Equal(422, result.AuthResult.HttpCode);
            dosFactoresMock.Verify(
                s => s.StartAsync(It.IsAny<AuthenticatedPerson>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EjecutarAsync_FallaElEnvioDel2Fa_PropagaElError()
        {
            var sut = CreateService(out _, out _, out var dosFactoresMock, out _);
            dosFactoresMock
                .Setup(s => s.StartAsync(It.IsAny<AuthenticatedPerson>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<TwoFactorRequiredResponse>.IsFailed(
                    "AUTH_2FA_MAIL", nameof(ITwoFactorAuthService.StartAsync), "No se pudo enviar el código.", 502, default!));

            var result = await sut.ExecuteAsync(CreateRequest(), "127.0.0.1", 0.4);

            Assert.False(result.RequiresTwoFactor);
            Assert.False(result.AuthResult!.Success);
            Assert.Equal("AUTH_2FA_MAIL", result.AuthResult.ErrorCode);
        }

        [Fact]
        public async Task EjecutarAsync_ConLoggingHabilitado_LogueaElLoginExitoso()
        {
            var sut = CreateService(out _, out _, out _, out _, VerboseLogger());

            var result = await sut.ExecuteAsync(CreateRequest(), "127.0.0.1", 0.9);

            Assert.True(result.AuthResult!.Success);
        }

        [Fact]
        public async Task EjecutarAsync_ConLoggingHabilitado_LogueaElInicioDel2Fa()
        {
            var sut = CreateService(out _, out _, out _, out _, VerboseLogger());

            var result = await sut.ExecuteAsync(CreateRequest(), "127.0.0.1", 0.4);

            Assert.True(result.RequiresTwoFactor);
        }

        [Fact]
        public async Task EjecutarAsync_SinConfiguracionDeLimites_UsaLosValoresPorDefecto()
        {
            var rateLimiterMock = new Mock<IRateLimiterService>();
            rateLimiterMock
                .Setup(s => s.ValidateAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(new RateLimitValidationResult
                {
                    IsAllowed = false,
                    RemainingAttempts = 0,
                    PartitionKey = "login:127.0.0.1:12345678"
                });

            var sut = new LoginFlowService(
                Mock.Of<IAuthenticateWithLdap>(),
                Mock.Of<IIssueTokensForPerson>(),
                rateLimiterMock.Object,
                Mock.Of<ITwoFactorAuthService>(),
                new ConfigurationBuilder().Build(),
                Mock.Of<ILogger<LoginFlowService>>());

            var result = await sut.ExecuteAsync(CreateRequest(), "127.0.0.1", 0.9);

            Assert.Equal("AUTH_RL_02", result.AuthResult!.ErrorCode);
            Assert.Equal(5, result.RateLimitHeaders!.Limit);
            rateLimiterMock.Verify(
                s => s.ValidateAsync(
                    "127.0.0.1", "CI", "12345678", 5, TimeSpan.FromMinutes(15)),
                Times.Once);
        }

        private static LoginFlowService CreateService(
            out Mock<IAuthenticateWithLdap> authenticateMock,
            out Mock<IIssueTokensForPerson> issueTokensMock,
            out Mock<ITwoFactorAuthService> dosFactoresMock,
            out Mock<IRateLimiterService> rateLimiterMock,
            ILogger<LoginFlowService>? logger = null)
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
                logger ?? Mock.Of<ILogger<LoginFlowService>>());
        }

        /// <summary>Logger que responde IsEnabled=true, para ejercitar los logs condicionales.</summary>
        private static ILogger<LoginFlowService> VerboseLogger()
        {
            var mock = new Mock<ILogger<LoginFlowService>>();
            mock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            return mock.Object;
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
