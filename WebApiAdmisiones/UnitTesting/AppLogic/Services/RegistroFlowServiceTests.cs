using System.Text.Json;
using AppLogic.DTOs;
using AppLogic.IServices;
using AppLogic.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using StackExchange.Redis;
using Utilities;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    [Collection(EnvironmentVariablesCollection.Name)]
    public class RegistroFlowServiceTests
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
                [pendingKey] = JsonSerializer.Serialize(new RegistroPendingPersona
                {
                    FlowId = oldFlowId,
                    TipoDocumento = "CI",
                    Documento = "12345672",
                    Email = "old@example.com",
                    TokenHash = "old-token-hash",
                    CreatedAt = DateTime.UtcNow
                }, JsonOptions),
                [flowSessionKey] = JsonSerializer.Serialize(new RegistroFlowSession
                {
                    FlowId = newFlowId,
                    TipoDocumento = "CI",
                    Documento = "12345672",
                    Step = "evaluado",
                    CreatedAt = DateTime.UtcNow
                }, JsonOptions)
            };
            var redisDbMock = CrearRedisMock(redis);
            var redisConnectionMock = new Mock<IConnectionMultiplexer>();
            redisConnectionMock
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(redisDbMock.Object);
            var registroServiceMock = new Mock<IRegistroService>();
            registroServiceMock
                .Setup(s => s.ValidarNuevaPersonaAsync(It.IsAny<RegistroPersonaRequest>()))
                .ReturnsAsync(OperationResult<object?>.Ok(default, nameof(IRegistroService.ValidarNuevaPersonaAsync)));
            var passwordActivationMock = new Mock<IPasswordActivationService>();
            passwordActivationMock
                .Setup(s => s.EnviarMailNuevaPersonaAsync(oldFlowId, "new@example.com", It.IsAny<string>()))
                .ReturnsAsync(OperationResult<object?>.IsSuccess(null, nameof(IPasswordActivationService.EnviarMailNuevaPersonaAsync), "OK"));
            var service = new RegistroFlowService(
                registroServiceMock.Object,
                passwordActivationMock.Object,
                CrearConfiguracion(),
                redisConnectionMock.Object);
            var request = CrearRegistroPersonaRequest("new@example.com");

            var result = await service.ConfirmarNuevaPersonaAsync(request, newFlowId);

            Assert.True(result.Success);
            passwordActivationMock.Verify(
                s => s.EnviarMailNuevaPersonaAsync(oldFlowId, "new@example.com", It.IsAny<string>()),
                Times.Once);
            Assert.Equal(oldFlowId, redis[docKey]);
            var pending = JsonSerializer.Deserialize<RegistroPendingPersona>(redis[pendingKey], JsonOptions);
            Assert.NotNull(pending);
            Assert.Equal(oldFlowId, pending!.FlowId);
            Assert.Equal("new@example.com", pending.Email);
            Assert.NotEqual("old-token-hash", pending.TokenHash);
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

        private static RegistroPersonaRequest CrearRegistroPersonaRequest(string mail)
            => new()
            {
                TipoDocumento = "CI",
                Documento = "12345672",
                IdProducto = 10,
                IdProceso = 20,
                PrimerApellido = "Perez",
                PrimerNombre = "Ana",
                FechaNacimiento = new DateTime(1990, 1, 1),
                Sexo = "F",
                Direccion = "Calle 1",
                Telefono1 = "099123456",
                Mail = mail,
                VerificacionMail = mail,
                CodigoPais = 1,
                CodigoEstado = 1,
                CodigoCiudad = 1
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
