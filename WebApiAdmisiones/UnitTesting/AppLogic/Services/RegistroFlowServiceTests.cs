using AppLogic.Registro.Dtos;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Utilities;
using Xunit;
using AppLogic.Registro.Services;
using AppLogic.Autenticacion.Interfaces;
using AppLogic.Registro.Interfaces;

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
                [pendingKey] = JsonSerializer.Serialize(new DtoRegistroPendingPersona
                {
                    FlowId = oldFlowId,
                    TipoDocumento = "CI",
                    Documento = "12345672",
                    Email = "old@example.com",
                    TokenHash = "old-token-hash",
                    CreatedAt = DateTime.UtcNow
                }, JsonOptions),
                [flowSessionKey] = JsonSerializer.Serialize(new DtoRegistroFlowSession
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
                .Setup(s => s.ValidarNuevaPersonaAsync(It.IsAny<DtoRegistroPersonaRequest>()))
                .ReturnsAsync(OperationResult<object?>.Ok(default, nameof(IRegistroService.ValidarNuevaPersonaAsync)));
            var passwordActivationMock = new Mock<IPasswordActivationService>();
            passwordActivationMock
                .Setup(s => s.GenerarTokenFlowId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns("generated-token");
            passwordActivationMock
                .Setup(s => s.EnviarMailNuevaPersonaAsync(oldFlowId, "new@example.com", It.IsAny<string>()))
                .ReturnsAsync(OperationResult<object?>.IsSuccess(null, nameof(IPasswordActivationService.EnviarMailNuevaPersonaAsync), "OK"));
            var service = new RegistroFlowService(
                registroServiceMock.Object,
                passwordActivationMock.Object,
                CrearConfiguracion(),
                redisConnectionMock.Object,
                Mock.Of<IRegistroDocumentoImagenCacheService>(),
                Mock.Of<ILogger<RegistroFlowService>>());
            var request = CrearRegistroPersonaRequest("new@example.com");

            var result = await service.ConfirmarNuevaPersonaAsync(request, newFlowId);

            Assert.True(result.Success);
            passwordActivationMock.Verify(
                s => s.EnviarMailNuevaPersonaAsync(oldFlowId, "new@example.com", It.IsAny<string>()),
                Times.Once);
            Assert.Equal(oldFlowId, redis[docKey]);
            var pending = JsonSerializer.Deserialize<DtoRegistroPendingPersona>(redis[pendingKey], JsonOptions);
            Assert.NotNull(pending);
            Assert.Equal(oldFlowId, pending!.FlowId);
            Assert.Equal("new@example.com", pending.Email);
            Assert.NotEqual("old-token-hash", pending.TokenHash);
        }

        [Fact]
        public async Task ConfirmarNuevaPersonaAsync_WhenMailFails_ReturnsSuccessWithMailEnviadoFalse()
        {
            // SRV-05: un fallo al enviar el mail de activación no debe convertir un registro
            // exitoso en error; el estado parcial va estructurado en MailEnviado.
            using var environment = new EnvironmentVariableScope(("PASSWORD_ACTIVATION_SECRET_KEY", Secret));
            var oldFlowId = "old-flow";
            var newFlowId = "new-flow";
            var docKey = "registro:pending-doc:CI:12345672";
            var pendingKey = $"registro:pending:{oldFlowId}";
            var flowSessionKey = $"registro:flow-session:{newFlowId}";
            var redis = new Dictionary<string, string>
            {
                [docKey] = oldFlowId,
                [pendingKey] = JsonSerializer.Serialize(new DtoRegistroPendingPersona
                {
                    FlowId = oldFlowId,
                    TipoDocumento = "CI",
                    Documento = "12345672",
                    Email = "old@example.com",
                    TokenHash = "old-token-hash",
                    CreatedAt = DateTime.UtcNow
                }, JsonOptions),
                [flowSessionKey] = JsonSerializer.Serialize(new DtoRegistroFlowSession
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
                .Setup(s => s.ValidarNuevaPersonaAsync(It.IsAny<DtoRegistroPersonaRequest>()))
                .ReturnsAsync(OperationResult<object?>.Ok(default, nameof(IRegistroService.ValidarNuevaPersonaAsync)));
            var passwordActivationMock = new Mock<IPasswordActivationService>();
            passwordActivationMock
                .Setup(s => s.GenerarTokenFlowId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns("generated-token");
            passwordActivationMock
                .Setup(s => s.EnviarMailNuevaPersonaAsync(oldFlowId, "new@example.com", It.IsAny<string>()))
                .ReturnsAsync(OperationResult<object?>.IsFailed(
                    "ACT_NUP_99",
                    nameof(IPasswordActivationService.EnviarMailNuevaPersonaAsync),
                    "No fue posible enviar el mail.",
                    500,
                    default));
            var service = new RegistroFlowService(
                registroServiceMock.Object,
                passwordActivationMock.Object,
                CrearConfiguracion(),
                redisConnectionMock.Object,
                Mock.Of<IRegistroDocumentoImagenCacheService>(),
                Mock.Of<ILogger<RegistroFlowService>>());
            var request = CrearRegistroPersonaRequest("new@example.com");

            var result = await service.ConfirmarNuevaPersonaAsync(request, newFlowId);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.False(result.Data!.MailEnviado);
        }

        [Fact]
        public async Task CompletarNuevaPersona_WithCachedImages_PassesImagesAndDeletesCacheOnSuccess()
        {
            var redisConnectionMock = CrearRedisConnectionMock([]);
            var registroServiceMock = new Mock<IRegistroService>();
            var passwordActivationMock = new Mock<IPasswordActivationService>();
            var cacheMock = new Mock<IRegistroDocumentoImagenCacheService>();
            var pending = CrearPendingPersona();
            var imagenes = CrearImagenesTemporales();
            cacheMock
                .Setup(c => c.ObtenerAsync("CI", "1234567-2"))
                .ReturnsAsync(imagenes);
            registroServiceMock
                .Setup(s => s.CompletarNuevaPersonaAsync(pending, "NuevaPassword1!", imagenes))
                .ReturnsAsync(OperationResult<long>.Ok(123, nameof(IRegistroService.CompletarNuevaPersonaAsync)));
            var service = new RegistroFlowService(
                registroServiceMock.Object,
                passwordActivationMock.Object,
                CrearConfiguracion(),
                redisConnectionMock.Object,
                cacheMock.Object,
                Mock.Of<ILogger<RegistroFlowService>>());

            var result = await service.CompletarNuevaPersona(pending, "NuevaPassword1!");

            Assert.True(result.Success);
            registroServiceMock.Verify(
                s => s.CompletarNuevaPersonaAsync(pending, "NuevaPassword1!", imagenes),
                Times.Once);
            cacheMock.Verify(c => c.ObtenerAsync("CI", "1234567-2"), Times.Once);
            cacheMock.Verify(c => c.EliminarAsync("CI", "1234567-2"), Times.Once);
        }

        [Fact]
        public async Task CompletarNuevaPersona_WhenCreationFails_DoesNotDeleteCachedImages()
        {
            var redisConnectionMock = CrearRedisConnectionMock([]);
            var registroServiceMock = new Mock<IRegistroService>();
            var passwordActivationMock = new Mock<IPasswordActivationService>();
            var cacheMock = new Mock<IRegistroDocumentoImagenCacheService>();
            var pending = CrearPendingPersona();
            var imagenes = CrearImagenesTemporales();
            cacheMock
                .Setup(c => c.ObtenerAsync("CI", "1234567-2"))
                .ReturnsAsync(imagenes);
            registroServiceMock
                .Setup(s => s.CompletarNuevaPersonaAsync(pending, "NuevaPassword1!", imagenes))
                .ReturnsAsync(OperationResult<long>.IsFailed(
                    "REG_PERSONA_99",
                    nameof(IRegistroService.CompletarNuevaPersonaAsync),
                    "Error",
                    500,
                    default));
            var service = new RegistroFlowService(
                registroServiceMock.Object,
                passwordActivationMock.Object,
                CrearConfiguracion(),
                redisConnectionMock.Object,
                cacheMock.Object,
                Mock.Of<ILogger<RegistroFlowService>>());

            var result = await service.CompletarNuevaPersona(pending, "NuevaPassword1!");

            Assert.False(result.Success);
            cacheMock.Verify(c => c.EliminarAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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

        private static DtoRegistroPersonaRequest CrearRegistroPersonaRequest(string mail)
            => new()
            {
                TipoDocumento = "CI",
                Documento = "12345672",
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

        private static DtoRegistroPendingPersona CrearPendingPersona()
            => new()
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                PrimerApellido = "Perez",
                PrimerNombre = "Ana",
                FechaNacimiento = new DateTime(1990, 1, 1),
                Sexo = "F",
                Direccion = "Calle 1",
                Telefono1 = "099123456",
                Email = "ana@example.com",
                CodigoPais = 1,
                CodigoEstado = 1,
                CodigoCiudad = 1
            };

        private static DtoRegistroDocumentoImagenesTemporales CrearImagenesTemporales()
            => new()
            {
                TipoDocumento = "CI",
                Documento = "1234567-2",
                DocumentoFrente = new DtoRegistroDocumentoArchivoTemporal
                {
                    Archivo = [0x25, 0x50, 0x44, 0x46, 1],
                    NombreArchivo = "documento.pdf",
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
