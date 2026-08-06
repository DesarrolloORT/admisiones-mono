using AppLogic.Authentication.Contracts;
using AppLogic.Identity;
using AppLogic.Identity.Dtos;
using AppLogic.Contracts.Text;
using AppLogic.Authentication.Dtos;
using AppLogic.Platform.Email;
using AppLogic.Platform.RateLimiting;
using AppLogic.Authentication.Interfaces;
using AppLogic.Authentication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;

namespace UnitTesting.Security
{
    /// <summary>
    /// Ambos servicios delegan en TextNormalization.NormalizeDocumentForKey para construir
    /// sus claves de rate limiting; este test cruza los dos flujos reales (sin mocks de
    /// IdentityDocumentRules) para confirmar que el segmento normalizado coincide para el mismo documento.
    /// </summary>
    public class RateLimitDocumentNormalizationConsistencyTests
    {
        [Theory]
        [InlineData("1.234.567-8")]
        [InlineData("1234567-8")]
        [InlineData("12345678")]
        [InlineData(" 1.234.567-8 ")]
        public async Task LoginFlowService_And_DosFactoresAuthService_NormalizeDocumentIdentically(string document)
        {
            var loginFlowKey = await CaptureLoginFlowFailedAttemptKeyAsync(document);
            var dosFactoresKey = await CaptureDosFactoresInitKeyAsync(document);

            var loginFlowSegment = loginFlowKey["login-fail-cred-user:".Length..];
            var dosFactoresSegment = dosFactoresKey["2fa-init:".Length..];

            Assert.Equal(dosFactoresSegment, loginFlowSegment);
        }

        private static async Task<string> CaptureLoginFlowFailedAttemptKeyAsync(string document)
        {
            string? capturedKey = null;

            var authenticateMock = new Mock<IAuthenticateWithLdap>();
            authenticateMock
                .Setup(s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<AuthenticatedPerson>.Ok(
                    new AuthenticatedPerson
                    {
                        PersonId = 123,
                        DocumentNumber = document,
                        Email = "test@example.com"
                    },
                    nameof(IAuthenticateWithLdap)));
            var issueTokensMock = new Mock<IIssueTokensForPerson>();
            issueTokensMock
                .Setup(s => s.ExecuteAsync(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync((long personId, string? _) => OperationResult<AuthenticationResponse>.Ok(
                    new AuthenticationResponse
                    {
                        Person = new AuthenticatedPerson { PersonId = personId, DocumentNumber = document },
                        AccessToken = "access-token",
                        RefreshToken = "refresh-token"
                    },
                    nameof(IIssueTokensForPerson)));

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

            var dosFactoresMock = new Mock<ITwoFactorAuthService>();
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

            var sut = new LoginFlowService(
                authenticateMock.Object,
                issueTokensMock.Object,
                rateLimiterMock.Object,
                dosFactoresMock.Object,
                configuration,
                Mock.Of<ILogger<LoginFlowService>>());

            await sut.ExecuteAsync(
                new AuthRequest { DocumentType = "CI", DocumentNumber = document, Password = "Password1!" },
                "127.0.0.1",
                0.9);

            Assert.NotNull(capturedKey);
            return capturedKey!;
        }

        private static async Task<string> CaptureDosFactoresInitKeyAsync(string document)
        {
            string? capturedKey = null;

            var sessionStoreMock = new Mock<ITwoFactorSessionStore>();
            sessionStoreMock
                .Setup(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<TwoFactorSession>(), It.IsAny<TimeSpan>()))
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

            var sut = new TwoFactorAuthService(
                sessionStoreMock.Object,
                rateLimiterMock.Object,
                emailSenderMock.Object,
                configuration,
                Mock.Of<ILogger<TwoFactorAuthService>>(),
                Mock.Of<IIssueTokensForPerson>());

            var pendingPersona = new AuthenticatedPerson
            {
                PersonId = 123,
                DocumentNumber = document,
                FirstName = "Gabriele",
                FirstSurname = "Test"
            };

            await sut.StartAsync(pendingPersona, "gabriele@ort.edu.uy");

            Assert.NotNull(capturedKey);
            return capturedKey!;
        }
    }
}
