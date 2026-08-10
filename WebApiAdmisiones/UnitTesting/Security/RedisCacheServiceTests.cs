using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Text.Json;
using WebApiAdmisiones.Security.Cache;
using Xunit;

namespace UnitTesting.Security
{
    /// <summary>
    /// El contrato del cache es "nunca romper el request": ante cualquier fallo de Redis o de
    /// serialización tiene que caer a ejecutar la factory igual.
    /// </summary>
    public class RedisCacheServiceTests
    {
        public class CatalogoFake
        {
            public int Id { get; set; }
            public string? Nombre { get; set; }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static Mock<ILogger<RedisCacheService>> LoggerMock(bool enabled = true)
        {
            var mock = new Mock<ILogger<RedisCacheService>>();
            mock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(enabled);
            return mock;
        }

        private static RedisCacheService Crear(Mock<IDatabase> dbMock, ILogger<RedisCacheService>? logger = null)
        {
            var connectionMock = new Mock<IConnectionMultiplexer>();
            connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);
            return new RedisCacheService(connectionMock.Object, logger ?? LoggerMock().Object);
        }

        private static Mock<IDatabase> DbConValor(RedisValue value)
        {
            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(value);
            dbMock.Setup(d => d.StringSetAsync(
                    It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
                    It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            return dbMock;
        }

        [Fact]
        public async Task GetOrSetAsync_CacheHit_DeserializaYNoEjecutaLaFactory()
        {
            var cacheado = JsonSerializer.Serialize(new CatalogoFake { Id = 7, Nombre = "Cacheado" }, JsonOptions);
            var service = Crear(DbConValor(cacheado));
            var factoryEjecutada = false;

            var result = await service.GetOrSetAsync<CatalogoFake>("k", () =>
            {
                factoryEjecutada = true;
                return Task.FromResult<CatalogoFake?>(new CatalogoFake { Id = 99 });
            }, TimeSpan.FromMinutes(5));

            Assert.False(factoryEjecutada);
            Assert.NotNull(result);
            Assert.Equal(7, result.Id);
            Assert.Equal("Cacheado", result.Nombre);
        }

        [Fact]
        public async Task GetOrSetAsync_CacheMiss_EjecutaFactoryYGuardaConElTtl()
        {
            var dbMock = DbConValor(RedisValue.Null);
            var service = Crear(dbMock);
            var ttl = TimeSpan.FromMinutes(10);

            var result = await service.GetOrSetAsync<CatalogoFake>(
                "k", () => Task.FromResult<CatalogoFake?>(new CatalogoFake { Id = 42, Nombre = "Fresco" }), ttl);

            Assert.NotNull(result);
            Assert.Equal(42, result.Id);
            dbMock.Verify(d => d.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == "k"),
                It.Is<RedisValue>(v => v.ToString().Contains("\"id\":42")),
                ttl,
                It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task GetOrSetAsync_CacheMissYFactoryDevuelveNull_NoGuardaNada()
        {
            var dbMock = DbConValor(RedisValue.Null);
            var service = Crear(dbMock);

            var result = await service.GetOrSetAsync<CatalogoFake>(
                "k", () => Task.FromResult<CatalogoFake?>(null), TimeSpan.FromMinutes(5));

            Assert.Null(result);
            dbMock.Verify(d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Never);
        }

        [Fact]
        public async Task GetOrSetAsync_LoggingDeshabilitado_SigueDevolviendoElValor()
        {
            var cacheado = JsonSerializer.Serialize(new CatalogoFake { Id = 7 }, JsonOptions);
            var service = Crear(DbConValor(cacheado), LoggerMock(enabled: false).Object);

            var result = await service.GetOrSetAsync<CatalogoFake>(
                "k", () => Task.FromResult<CatalogoFake?>(null), TimeSpan.FromMinutes(5));

            Assert.NotNull(result);
            Assert.Equal(7, result.Id);
        }

        [Fact]
        public async Task GetOrSetAsync_RedisCaido_CaeALaFactory()
        {
            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "redis caido"));
            var service = Crear(dbMock);

            var result = await service.GetOrSetAsync<CatalogoFake>(
                "k", () => Task.FromResult<CatalogoFake?>(new CatalogoFake { Id = 1 }), TimeSpan.FromMinutes(5));

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetOrSetAsync_JsonCorruptoEnCache_CaeALaFactory()
        {
            var service = Crear(DbConValor("{ esto no es json"));

            var result = await service.GetOrSetAsync<CatalogoFake>(
                "k", () => Task.FromResult<CatalogoFake?>(new CatalogoFake { Id = 5 }), TimeSpan.FromMinutes(5));

            Assert.NotNull(result);
            Assert.Equal(5, result.Id);
        }

        [Fact]
        public async Task GetOrSetAsync_ErrorInesperado_CaeALaFactory()
        {
            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ThrowsAsync(new InvalidOperationException("boom"));
            var service = Crear(dbMock);

            var result = await service.GetOrSetAsync<CatalogoFake>(
                "k", () => Task.FromResult<CatalogoFake?>(new CatalogoFake { Id = 3 }), TimeSpan.FromMinutes(5));

            Assert.NotNull(result);
            Assert.Equal(3, result.Id);
        }

        [Fact]
        public async Task InvalidateAsync_ClaveExistente_DevuelveTrue()
        {
            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

            Assert.True(await Crear(dbMock).InvalidateAsync("k"));
        }

        [Fact]
        public async Task InvalidateAsync_ClaveInexistente_DevuelveFalse()
        {
            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(false);

            Assert.False(await Crear(dbMock).InvalidateAsync("k"));
        }

        [Fact]
        public async Task InvalidateAsync_ErrorDeRedis_DevuelveFalseSinPropagar()
        {
            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "redis caido"));

            Assert.False(await Crear(dbMock).InvalidateAsync("k"));
        }

        [Fact]
        public async Task GetTtlAsync_DevuelveElTiempoRestante()
        {
            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(TimeSpan.FromMinutes(3));

            Assert.Equal(TimeSpan.FromMinutes(3), await Crear(dbMock).GetTtlAsync("k"));
        }

        [Fact]
        public async Task GetTtlAsync_ErrorDeRedis_DevuelveNullSinPropagar()
        {
            var dbMock = new Mock<IDatabase>();
            dbMock.Setup(d => d.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "redis caido"));

            Assert.Null(await Crear(dbMock).GetTtlAsync("k"));
        }
    }
}
