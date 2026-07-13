using System.Text.Json;
using AppLogic.Common.Serialization;
using AppLogic.Registro.Dtos;
using AppLogic.Registro.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class RegistroDocumentoImagenCacheServiceTests
    {
        private static IConfiguration CrearConfiguracion()
            => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        private static DtoRegistroDocumentoArchivoTemporal CrearDocumentoFrente()
            => new()
            {
                Archivo = [0x25, 0x50, 0x44, 0x46, 1],
                NombreArchivo = "documento.pdf",
                ContentType = "application/pdf"
            };

        [Fact]
        public async Task GuardarImagenesTemporalesSiCorrespondeAsync_WithValidData_SavesUnderExpectedKeyAndTtl()
        {
            RedisKey? capturedKey = null;
            RedisValue capturedValue = default;
            TimeSpan? capturedTtl = null;
            var redisDbMock = new Mock<IDatabase>();
            redisDbMock
                .Setup(d => d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<bool>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()))
                .Callback<RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags>(
                    (key, value, ttl, _, _, _) =>
                    {
                        capturedKey = key;
                        capturedValue = value;
                        capturedTtl = ttl;
                    })
                .ReturnsAsync(true);
            var connectionMock = new Mock<IConnectionMultiplexer>();
            connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDbMock.Object);
            var service = new RegistroDocumentoImagenCacheService(CrearConfiguracion(), connectionMock.Object);

            await service.GuardarImagenesTemporalesSiCorrespondeAsync(
                "CI",
                "12345672",
                new DateTime(2030, 1, 1),
                CrearDocumentoFrente(),
                null);

            Assert.Equal("registro:documento-imagenes:CI:12345672", capturedKey.ToString());
            Assert.Equal(TimeSpan.FromHours(24), capturedTtl);
            var payload = JsonSerializer.Deserialize<DtoRegistroDocumentoImagenesTemporales>(
                capturedValue.ToString(),
                JsonSerializationDefaults.Redis);
            Assert.NotNull(payload);
            Assert.Equal("CI", payload!.TipoDocumento);
            Assert.Equal("12345672", payload.Documento);
            Assert.Equal(new DateTime(2030, 1, 1), payload.FechaVencimiento);
            Assert.Equal("documento.pdf", payload.DocumentoFrente.NombreArchivo);
            Assert.Null(payload.CaraPersona);
        }

        [Fact]
        public async Task GuardarImagenesTemporalesSiCorrespondeAsync_WithCaraPersona_IncludesItInPayload()
        {
            RedisValue capturedValue = default;
            var redisDbMock = new Mock<IDatabase>();
            redisDbMock
                .Setup(d => d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<bool>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()))
                .Callback<RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags>(
                    (_, value, _, _, _, _) => capturedValue = value)
                .ReturnsAsync(true);
            var connectionMock = new Mock<IConnectionMultiplexer>();
            connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDbMock.Object);
            var service = new RegistroDocumentoImagenCacheService(CrearConfiguracion(), connectionMock.Object);
            var caraPersona = new DtoRegistroDocumentoArchivoTemporal
            {
                Archivo = [0xFF, 0xD8, 0xFF, 0xE0, 1],
                NombreArchivo = "cara.jpg",
                ContentType = "image/jpeg"
            };

            await service.GuardarImagenesTemporalesSiCorrespondeAsync(
                "CI",
                "12345672",
                null,
                CrearDocumentoFrente(),
                caraPersona);

            var payload = JsonSerializer.Deserialize<DtoRegistroDocumentoImagenesTemporales>(
                capturedValue.ToString(),
                JsonSerializationDefaults.Redis);
            Assert.NotNull(payload!.CaraPersona);
            Assert.Equal("cara.jpg", payload.CaraPersona!.NombreArchivo);
        }

        [Theory]
        [InlineData(null, "12345672")]
        [InlineData("", "12345672")]
        [InlineData("   ", "12345672")]
        [InlineData("CI", null)]
        [InlineData("CI", "")]
        [InlineData("CI", "   ")]
        public async Task GuardarImagenesTemporalesSiCorrespondeAsync_WithBlankTipoOrNumero_DoesNotWriteToRedis(
            string? tipoDocumento,
            string? numeroDocumento)
        {
            var redisDbMock = new Mock<IDatabase>();
            var connectionMock = new Mock<IConnectionMultiplexer>();
            connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDbMock.Object);
            var service = new RegistroDocumentoImagenCacheService(CrearConfiguracion(), connectionMock.Object);

            await service.GuardarImagenesTemporalesSiCorrespondeAsync(
                tipoDocumento,
                numeroDocumento,
                null,
                CrearDocumentoFrente(),
                null);

            redisDbMock.Verify(
                d => d.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<bool>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()),
                Times.Never);
        }

        [Fact]
        public async Task GuardarImagenesTemporalesSiCorrespondeAsync_WhenRedisFails_DoesNotThrow()
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
                .ThrowsAsync(new InvalidOperationException("Redis unavailable"));
            var connectionMock = new Mock<IConnectionMultiplexer>();
            connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDbMock.Object);
            var service = new RegistroDocumentoImagenCacheService(CrearConfiguracion(), connectionMock.Object);

            var exception = await Record.ExceptionAsync(() => service.GuardarImagenesTemporalesSiCorrespondeAsync(
                "CI",
                "12345672",
                null,
                CrearDocumentoFrente(),
                null));

            Assert.Null(exception);
        }
    }
}
