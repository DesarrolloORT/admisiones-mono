using AppLogic.DTOs;
using MailORT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using WebApiAdmisiones.Security.Authentication;
using WebApiAdmisiones.Security.RateLimiting;

namespace UnitTesting.Security
{
    public class DosFactoresAuthServiceTests
    {
        [Fact]
        public async Task IniciarAsync_WhenMailIsSent_ReturnsMaskedEmailAndUsesRealEmailForDelivery()
        {
            var redis = CreateRedisHarness();
            var mail = new TestableEnvioMail();
            var service = CreateService(redis, mail);

            var result = await service.IniciarAsync(CreatePendingAuth(), "gabriele@ort.edu.uy");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("g******e@ort.******", result.Data.MaskedEmail);
            Assert.NotEqual("gabriele@ort.edu.uy", result.Data.MaskedEmail);
            Assert.Contains("gabriele@ort.edu.uy", mail.To);
            Assert.Equal(1, mail.SendCount);

            var json = JsonSerializer.Serialize(result.Data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            Assert.Contains("\"maskedEmail\":\"g******e@ort.******\"", json);
            Assert.DoesNotContain("gabriele@ort.edu.uy", json);
        }

        [Fact]
        public async Task ReenviarCodigoAsync_WhenSessionExists_UpdatesCodeResetsAttemptsSendsMailAndPreservesTtl()
        {
            var redis = CreateRedisHarness();
            var mail = new TestableEnvioMail();
            var service = CreateService(redis, mail);

            var start = await service.IniciarAsync(CreatePendingAuth(), "gabriele@ort.edu.uy");
            var sessionId = start.Data!.SessionId;
            var sessionKey = $"2fa:session:{sessionId}";
            var remainingTtl = TimeSpan.FromMinutes(17);

            var before = JsonNode.Parse(redis.Store[sessionKey])!.AsObject();
            before["intentos"] = 3;
            redis.Store[sessionKey] = before.ToJsonString();
            redis.Ttls[sessionKey] = remainingTtl;
            var oldHash = before["codigoHash"]!.GetValue<string>();

            var result = await service.ReenviarCodigoAsync(sessionId);

            Assert.True(result.Success);
            Assert.Equal(sessionId, result.Data!.SessionId);
            Assert.Equal("g******e@ort.******", result.Data.MaskedEmail);
            Assert.Equal(2, mail.SendCount);
            Assert.Contains("gabriele@ort.edu.uy", mail.To);

            var after = JsonNode.Parse(redis.Store[sessionKey])!.AsObject();
            Assert.NotEqual(oldHash, after["codigoHash"]!.GetValue<string>());
            Assert.Equal(0, after["intentos"]!.GetValue<int>());
            Assert.Equal(remainingTtl, redis.Ttls[sessionKey]);

            var expiresAt = DateTime.Parse(
                after["codigoExpiresAtUtc"]!.GetValue<string>(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
            Assert.True(expiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task ReenviarCodigoAsync_WhenSuccessful_InvalidatesPreviousCode()
        {
            var redis = CreateRedisHarness();
            var mail = new TestableEnvioMail();
            var service = CreateService(redis, mail);

            var start = await service.IniciarAsync(CreatePendingAuth(), "gabriele@ort.edu.uy");
            var sessionId = start.Data!.SessionId;
            var oldCode = ExtractCode(mail.Body);

            var resend = await service.ReenviarCodigoAsync(sessionId);
            var newCode = ExtractCode(mail.Body);

            Assert.True(resend.Success);
            Assert.NotEqual(oldCode, newCode);

            var oldCodeResult = await service.VerificarCodigoAsync(sessionId, oldCode);
            Assert.False(oldCodeResult.Success);
            Assert.Equal("AUTH_2FA_05", oldCodeResult.ErrorCode);

            var newCodeResult = await service.VerificarCodigoAsync(sessionId, newCode);
            Assert.True(newCodeResult.Success);
        }

        [Fact]
        public async Task VerificarCodigoAsync_WhenCodeExpired_ReturnsUnauthorizedEvenIfSessionExists()
        {
            var redis = CreateRedisHarness();
            var mail = new TestableEnvioMail();
            var service = CreateService(redis, mail);

            var start = await service.IniciarAsync(CreatePendingAuth(), "gabriele@ort.edu.uy");
            var sessionId = start.Data!.SessionId;
            var sessionKey = $"2fa:session:{sessionId}";
            var code = ExtractCode(mail.Body);

            var session = JsonNode.Parse(redis.Store[sessionKey])!.AsObject();
            session["codigoExpiresAtUtc"] = DateTime.UtcNow.AddMinutes(-1);
            redis.Store[sessionKey] = session.ToJsonString();

            var result = await service.VerificarCodigoAsync(sessionId, code);

            Assert.False(result.Success);
            Assert.Equal(401, result.HttpCode);
            Assert.Equal("AUTH_2FA_06", result.ErrorCode);
            Assert.True(redis.Store.ContainsKey(sessionKey));
        }

        [Fact]
        public async Task ReenviarCodigoAsync_WhenSessionDoesNotExist_ReturnsUnauthorized()
        {
            var service = CreateService(CreateRedisHarness(), new TestableEnvioMail());

            var result = await service.ReenviarCodigoAsync("missing-session");

            Assert.False(result.Success);
            Assert.Equal(401, result.HttpCode);
            Assert.Equal("AUTH_2FA_02", result.ErrorCode);
        }

        [Fact]
        public async Task VerificarCodigoAsync_WhenCodeIsCorrect_ClearsTwoFactorInitRateLimit()
        {
            var redis = CreateRedisHarness();
            var mail = new TestableEnvioMail();
            var rateLimiterMock = new Mock<IRedisRateLimiterService>();
            var service = CreateService(redis, mail, rateLimiterMock);

            var start = await service.IniciarAsync(CreatePendingAuth(), "gabriele@ort.edu.uy");
            var code = ExtractCode(mail.Body);

            var result = await service.VerificarCodigoAsync(start.Data!.SessionId, code);

            Assert.True(result.Success);
            rateLimiterMock.Verify(r => r.ClearAsync("2fa-init:12345678"), Times.Once);
        }

        private static DtoAuthenticationResponse CreatePendingAuth() =>
            new()
            {
                Persona = new DtoPersonaAuth
                {
                    CodigoPersona = 123,
                    Documento = "1.234.567-8",
                    PrimerNombre = "Gabriele",
                    PrimerApellido = "Test"
                },
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                RefreshTokenHash = "refresh-token-hash"
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
            RedisHarness redis,
            TestableEnvioMail mail,
            Mock<IRedisRateLimiterService>? rateLimiterMock = null)
        {
            rateLimiterMock ??= new Mock<IRedisRateLimiterService>();
            rateLimiterMock
                .Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            rateLimiterMock
                .Setup(r => r.ClearAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            return new DosFactoresAuthService(
                redis.Connection.Object,
                rateLimiterMock.Object,
                mail,
                CreateConfiguration(),
                Mock.Of<ILogger<DosFactoresAuthService>>());
        }

        private static RedisHarness CreateRedisHarness()
        {
            var harness = new RedisHarness();

            harness.Database
                .Setup(d => d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<bool>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()))
                .Callback<RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags>(
                    (key, value, expiry, _, _, _) =>
                    {
                        var keyText = key.ToString();
                        harness.Store[keyText] = value.ToString();
                        harness.Ttls[keyText] = expiry;
                    })
                .ReturnsAsync(true);

            harness.Database
                .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns<RedisKey, CommandFlags>((key, _) =>
                {
                    var keyText = key.ToString();
                    return Task.FromResult(
                        harness.Store.TryGetValue(keyText, out var value)
                            ? (RedisValue)value
                            : RedisValue.Null);
                });

            harness.Database
                .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Callback<RedisKey, CommandFlags>((key, _) =>
                {
                    var keyText = key.ToString();
                    harness.Store.Remove(keyText);
                    harness.Ttls.Remove(keyText);
                })
                .ReturnsAsync(true);

            harness.Database
                .Setup(d => d.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns<RedisKey, CommandFlags>((key, _) =>
                {
                    var keyText = key.ToString();
                    harness.Ttls.TryGetValue(keyText, out var ttl);
                    return Task.FromResult(ttl);
                });

            harness.Connection
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(harness.Database.Object);

            return harness;
        }

        private static string ExtractCode(string body)
        {
            var match = Regex.Match(body, @"<strong[^>]*>(\d{6})</strong>");
            Assert.True(match.Success, $"No se encontro codigo 2FA en el body: {body}");
            return match.Groups[1].Value;
        }

        private sealed class RedisHarness
        {
            public Mock<IDatabase> Database { get; } = new();
            public Mock<IConnectionMultiplexer> Connection { get; } = new();
            public Dictionary<string, string> Store { get; } = [];
            public Dictionary<string, TimeSpan?> Ttls { get; } = [];
        }

        private class TestableEnvioMail : EnvioMail
        {
            public List<string> To { get; private set; } = [];
            public string Body { get; private set; } = string.Empty;
            public int SendCount { get; private set; }

            public TestableEnvioMail() : base("http://localhost/wsdl")
            {
            }

            public override Task EnviarMail(
                string from,
                List<string> colTOs,
                string subject,
                string body,
                List<string>? colReplyTo = null,
                string sistema = "")
            {
                To = colTOs;
                Body = body;
                SendCount++;
                return Task.CompletedTask;
            }
        }
    }
}
