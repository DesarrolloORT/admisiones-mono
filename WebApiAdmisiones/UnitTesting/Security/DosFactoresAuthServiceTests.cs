using AppLogic.DTOs;
using MailORT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Text.Json;
using WebApiAdmisiones.Security;
using WebApiAdmisiones.Security.interfaces;

namespace UnitTesting.Security
{
    public class DosFactoresAuthServiceTests
    {
        [Fact]
        public async Task IniciarAsync_WhenMailIsSent_ReturnsMaskedEmailAndUsesRealEmailForDelivery()
        {
            var redisDbMock = new Mock<IDatabase>();
            redisDbMock
                .Setup(d => d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<bool>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            var redisMock = new Mock<IConnectionMultiplexer>();
            redisMock
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(redisDbMock.Object);

            var rateLimiterMock = new Mock<IRedisRateLimiterService>();
            rateLimiterMock
                .Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);

            var mail = new TestableEnvioMail();
            var service = new DosFactoresAuthService(
                redisMock.Object,
                rateLimiterMock.Object,
                mail,
                CreateConfiguration(),
                Mock.Of<ILogger<DosFactoresAuthService>>());

            var result = await service.IniciarAsync(CreatePendingAuth(), "gabriele@ort.edu.uy");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("g******e@ort.******", result.Data.MaskedEmail);
            Assert.NotEqual("gabriele@ort.edu.uy", result.Data.MaskedEmail);
            Assert.Contains("gabriele@ort.edu.uy", mail.To);

            var json = JsonSerializer.Serialize(result.Data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            Assert.Contains("\"maskedEmail\":\"g******e@ort.******\"", json);
            Assert.DoesNotContain("gabriele@ort.edu.uy", json);
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
                    ["Authentication:TwoFactor:SessionMinutes"] = "10",
                    ["Authentication:TwoFactor:CodeLength"] = "6",
                    ["Mail:From"] = "admisiones@example.com"
                })
                .Build();

        private class TestableEnvioMail : EnvioMail
        {
            public List<string> To { get; private set; } = [];

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
                return Task.CompletedTask;
            }
        }
    }
}
