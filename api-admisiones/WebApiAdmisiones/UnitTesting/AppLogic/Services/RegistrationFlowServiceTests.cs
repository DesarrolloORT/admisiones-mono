using AppLogic.Contracts.Dtos;
using AppLogic.Identity.Services;
using AppLogic.Identity.Interfaces;
using AppLogic.Identity.Dtos;
using AppLogic.Registration.Dtos;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Utilities;
using Xunit;
using AppLogic.Registration.Services;
using AppLogic.Authentication.Interfaces;
using AppLogic.Registration.Interfaces;
using AppLogic.Registration.Contracts;
using AppLogic.Registration.UseCases;

namespace UnitTesting.AppLogic.Services
{
    [Collection(EnvironmentVariablesCollection.Name)]
    public class RegistrationFlowServiceTests
    {
        private const string Secret = "12345678901234567890123456789012";

        [Fact]
        public async Task ConfirmarNuevaPersonaAsync_WithExistingPendingForDocumento_UpdatesPendingAndInvalidatesOldToken()
        {
            using var environment = new EnvironmentVariableScope(("PASSWORD_ACTIVATION_SECRET_KEY", Secret));
            var oldFlowId = "old-flow";
            var newFlowId = "new-flow";
            var docKey = "registro:pending-doc:CI:12345672";
            var pendingKey = $"registro:pending:{oldFlowId}";
            var flowSessionKey = $"registro:flow-session:{newFlowId}";
            var redis = new Dictionary<string, string>
            {
                [docKey] = oldFlowId,
                [pendingKey] = JsonSerializer.Serialize(new PendingPerson
                {
                    FlowId = oldFlowId,
                    DocumentType = "CI",
                    DocumentNumber = "12345672",
                    Email = "old@example.com",
                    TokenHash = "old-token-hash",
                    CreatedAt = DateTime.UtcNow
                }, JsonOptions),
                [flowSessionKey] = JsonSerializer.Serialize(new RegistrationFlowSession
                {
                    FlowId = newFlowId,
                    DocumentType = "CI",
                    DocumentNumber = "12345672",
                    Step = "evaluado",
                    CreatedAt = DateTime.UtcNow
                }, JsonOptions)
            };
            var redisDbMock = CrearRedisMock(redis);
            var redisConnectionMock = new Mock<IConnectionMultiplexer>();
            redisConnectionMock
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(redisDbMock.Object);
            var validateNewPersonMock = new Mock<IValidateNewPerson>();
            var completeNewPersonMock = new Mock<ICompleteNewPerson>();
            validateNewPersonMock
                .Setup(s => s.ExecuteAsync(It.IsAny<RegisterPersonRequest>()))
                .ReturnsAsync(OperationResult<object?>.Ok(default, nameof(ValidateNewPerson)));
            var passwordActivationMock = new Mock<IPasswordActivationService>();
            passwordActivationMock
                .Setup(s => s.GenerateFlowIdToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns("generated-token");
            passwordActivationMock
                .Setup(s => s.SendNewPersonMailAsync(oldFlowId, "new@example.com", It.IsAny<string>()))
                .ReturnsAsync(OperationResult<object?>.IsSuccess(null, nameof(IPasswordActivationService.SendNewPersonMailAsync), "OK"));
            var service = new RegistrationFlowService(
                validateNewPersonMock.Object,
                completeNewPersonMock.Object,
                passwordActivationMock.Object,
                CrearConfiguracion(),
                redisConnectionMock.Object,
                Mock.Of<IIdentityDocumentImageCache>(),
                new RedisPendingPersonStore(redisConnectionMock.Object),
                Mock.Of<ILogger<RegistrationFlowService>>());
            var request = CrearRegistroPersonaRequest("new@example.com");

            var result = await service.ConfirmNewPersonAsync(request, newFlowId);

            Assert.True(result.Success);
            passwordActivationMock.Verify(
                s => s.SendNewPersonMailAsync(oldFlowId, "new@example.com", It.IsAny<string>()),
                Times.Once);
            Assert.Equal(oldFlowId, redis[docKey]);
            var pending = JsonSerializer.Deserialize<PendingPerson>(redis[pendingKey], JsonOptions);
            Assert.NotNull(pending);
            Assert.Equal(oldFlowId, pending!.FlowId);
            Assert.Equal("new@example.com", pending.Email);
            Assert.NotEqual("old-token-hash", pending.TokenHash);
        }

        [Fact]
        public async Task ConfirmarNuevaPersonaAsync_WhenMailFails_ReturnsSuccessWithMailEnviadoFalse()
        {
            // SRV-05: un fallo al enviar el mail de activación no debe convertir un registro
            // exitoso en error; el estado parcial va estructurado en MailSent.
            using var environment = new EnvironmentVariableScope(("PASSWORD_ACTIVATION_SECRET_KEY", Secret));
            var oldFlowId = "old-flow";
            var newFlowId = "new-flow";
            var docKey = "registro:pending-doc:CI:12345672";
            var pendingKey = $"registro:pending:{oldFlowId}";
            var flowSessionKey = $"registro:flow-session:{newFlowId}";
            var redis = new Dictionary<string, string>
            {
                [docKey] = oldFlowId,
                [pendingKey] = JsonSerializer.Serialize(new PendingPerson
                {
                    FlowId = oldFlowId,
                    DocumentType = "CI",
                    DocumentNumber = "12345672",
                    Email = "old@example.com",
                    TokenHash = "old-token-hash",
                    CreatedAt = DateTime.UtcNow
                }, JsonOptions),
                [flowSessionKey] = JsonSerializer.Serialize(new RegistrationFlowSession
                {
                    FlowId = newFlowId,
                    DocumentType = "CI",
                    DocumentNumber = "12345672",
                    Step = "evaluado",
                    CreatedAt = DateTime.UtcNow
                }, JsonOptions)
            };
            var redisDbMock = CrearRedisMock(redis);
            var redisConnectionMock = new Mock<IConnectionMultiplexer>();
            redisConnectionMock
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(redisDbMock.Object);
            var validateNewPersonMock = new Mock<IValidateNewPerson>();
            var completeNewPersonMock = new Mock<ICompleteNewPerson>();
            validateNewPersonMock
                .Setup(s => s.ExecuteAsync(It.IsAny<RegisterPersonRequest>()))
                .ReturnsAsync(OperationResult<object?>.Ok(default, nameof(ValidateNewPerson)));
            var passwordActivationMock = new Mock<IPasswordActivationService>();
            passwordActivationMock
                .Setup(s => s.GenerateFlowIdToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns("generated-token");
            passwordActivationMock
                .Setup(s => s.SendNewPersonMailAsync(oldFlowId, "new@example.com", It.IsAny<string>()))
                .ReturnsAsync(OperationResult<object?>.IsFailed(
                    "ACT_NUP_99",
                    nameof(IPasswordActivationService.SendNewPersonMailAsync),
                    "No fue posible enviar el mail.",
                    500,
                    default));
            var service = new RegistrationFlowService(
                validateNewPersonMock.Object,
                completeNewPersonMock.Object,
                passwordActivationMock.Object,
                CrearConfiguracion(),
                redisConnectionMock.Object,
                Mock.Of<IIdentityDocumentImageCache>(),
                new RedisPendingPersonStore(redisConnectionMock.Object),
                Mock.Of<ILogger<RegistrationFlowService>>());
            var request = CrearRegistroPersonaRequest("new@example.com");

            var result = await service.ConfirmNewPersonAsync(request, newFlowId);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.False(result.Data!.MailSent);
        }

        [Fact]
        public async Task CompletarNuevaPersona_WithCachedImages_PassesImagesAndDeletesCacheOnSuccess()
        {
            var redisConnectionMock = CrearRedisConnectionMock([]);
            var validateNewPersonMock = new Mock<IValidateNewPerson>();
            var completeNewPersonMock = new Mock<ICompleteNewPerson>();
            var passwordActivationMock = new Mock<IPasswordActivationService>();
            var cacheMock = new Mock<IIdentityDocumentImageCache>();
            var pending = CrearPendingPersona();
            var images = CrearImagenesTemporales();
            cacheMock
                .Setup(c => c.GetAsync("CI", "1234567-2"))
                .ReturnsAsync(images);
            completeNewPersonMock
                .Setup(s => s.ExecuteAsync(pending, "NuevaPassword1!", images))
                .ReturnsAsync(OperationResult<long>.Ok(123, nameof(CompleteNewPerson)));
            var service = new RegistrationFlowService(
                validateNewPersonMock.Object,
                completeNewPersonMock.Object,
                passwordActivationMock.Object,
                CrearConfiguracion(),
                redisConnectionMock.Object,
                cacheMock.Object,
                Mock.Of<IPendingPersonStore>(),
                Mock.Of<ILogger<RegistrationFlowService>>());

            var result = await service.CreatePersonFromPendingAsync(pending, "NuevaPassword1!");

            Assert.True(result.Success);
            completeNewPersonMock.Verify(
                s => s.ExecuteAsync(pending, "NuevaPassword1!", images),
                Times.Once);
            cacheMock.Verify(c => c.GetAsync("CI", "1234567-2"), Times.Once);
            cacheMock.Verify(c => c.DeleteAsync("CI", "1234567-2"), Times.Once);
        }

        [Fact]
        public async Task CompletarNuevaPersona_WhenCreationFails_DoesNotDeleteCachedImages()
        {
            var redisConnectionMock = CrearRedisConnectionMock([]);
            var validateNewPersonMock = new Mock<IValidateNewPerson>();
            var completeNewPersonMock = new Mock<ICompleteNewPerson>();
            var passwordActivationMock = new Mock<IPasswordActivationService>();
            var cacheMock = new Mock<IIdentityDocumentImageCache>();
            var pending = CrearPendingPersona();
            var images = CrearImagenesTemporales();
            cacheMock
                .Setup(c => c.GetAsync("CI", "1234567-2"))
                .ReturnsAsync(images);
            completeNewPersonMock
                .Setup(s => s.ExecuteAsync(pending, "NuevaPassword1!", images))
                .ReturnsAsync(OperationResult<long>.IsFailed(
                    "REG_PERSONA_99",
                    nameof(CompleteNewPerson),
                    "Error",
                    500,
                    default));
            var service = new RegistrationFlowService(
                validateNewPersonMock.Object,
                completeNewPersonMock.Object,
                passwordActivationMock.Object,
                CrearConfiguracion(),
                redisConnectionMock.Object,
                cacheMock.Object,
                Mock.Of<IPendingPersonStore>(),
                Mock.Of<ILogger<RegistrationFlowService>>());

            var result = await service.CreatePersonFromPendingAsync(pending, "NuevaPassword1!");

            Assert.False(result.Success);
            cacheMock.Verify(c => c.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private static Mock<IDatabase> CrearRedisMock(Dictionary<string, string> redis)
        {
            var redisDbMock = new Mock<IDatabase>();
            redisDbMock
                .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns<RedisKey, CommandFlags>((key, _) =>
                    Task.FromResult(redis.TryGetValue(key.ToString(), out var value)
                        ? (RedisValue)value
                        : RedisValue.Null));
            redisDbMock
                .Setup(d => d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<bool>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()))
                .Callback<RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags>((key, value, _, _, _, _) =>
                    redis[key.ToString()] = value.ToString())
                .ReturnsAsync(true);
            redisDbMock
                .Setup(d => d.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(TimeSpan.FromMinutes(30));
            redisDbMock
                .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Callback<RedisKey, CommandFlags>((key, _) => redis.Remove(key.ToString()))
                .ReturnsAsync(true);
            return redisDbMock;
        }

        private static Mock<IConnectionMultiplexer> CrearRedisConnectionMock(Dictionary<string, string> redis)
        {
            var redisDbMock = CrearRedisMock(redis);
            var redisConnectionMock = new Mock<IConnectionMultiplexer>();
            redisConnectionMock
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(redisDbMock.Object);
            return redisConnectionMock;
        }

        private static RegisterPersonRequest CrearRegistroPersonaRequest(string mail)
            => new()
            {
                DocumentType = "CI",
                DocumentNumber = "12345672",
                FirstSurname = "Perez",
                FirstName = "Ana",
                BirthDate = new DateTime(1990, 1, 1),
                Sex = "F",
                Address = "Calle 1",
                PrimaryPhone = new PhoneNumber { NationalNumber = "099123456", Iso2 = "UY" },
                Email = mail,
                EmailConfirmation = mail,
                CountryId = 1,
                StateId = 1,
                CityId = 1
            };

        private static PendingPerson CrearPendingPersona()
            => new()
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                FirstSurname = "Perez",
                FirstName = "Ana",
                BirthDate = new DateTime(1990, 1, 1),
                Sex = "F",
                Address = "Calle 1",
                // En Redis el teléfono ya viaja en E.164.
                PrimaryPhone = "+59899123456",
                Email = "ana@example.com",
                CountryId = 1,
                StateId = 1,
                CityId = 1
            };

        private static TemporaryDocumentImages CrearImagenesTemporales()
            => new()
            {
                DocumentType = "CI",
                DocumentNumber = "1234567-2",
                DocumentFront = new TemporaryDocumentFile
                {
                    Content = [0x25, 0x50, 0x44, 0x46, 1],
                    FileName = "documento.pdf",
                    ContentType = "application/pdf"
                }
            };

        private static IConfiguration CrearConfiguracion()
            => new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PasswordActivation:ExpireHours"] = "24",
                    ["Registro:FlowSessionMinutes"] = "30"
                })
                .Build();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
