using AppLogic.Authentication.Dtos;
using AppLogic.Authentication.Services;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class RedisTwoFactorSessionStoreTests
    {
        private const string KeyPrefix = "2fa:session:";

        private static TwoFactorSession CrearSesion() => new()
        {
            PersonId = 12345,
            FirstName = "Ana",
            FirstSurname = "Perez",
            DocumentNumber = "1234567-2",
            CodeHash = "hash",
            CodeExpiresAtUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Email = "ana@example.com",
            Attempts = 1
        };

        private static (RedisTwoFactorSessionStore Store, Mock<IDatabase> Db) CrearStore(Dictionary<string, string> redis)
        {
            var dbMock = new Mock<IDatabase>();

            dbMock
                .Setup(d => d.StringSetAsync(
                    It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
                    It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Callback<RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags>(
                    (key, value, _, _, _, _) => redis[key.ToString()] = value.ToString())
                .ReturnsAsync(true);

            dbMock
                .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns<RedisKey, CommandFlags>((key, _) =>
                    Task.FromResult(redis.TryGetValue(key.ToString(), out var value)
                        ? (RedisValue)value
                        : RedisValue.Null));

            dbMock
                .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Callback<RedisKey, CommandFlags>((key, _) => redis.Remove(key.ToString()))
                .ReturnsAsync(true);

            var connectionMock = new Mock<IConnectionMultiplexer>();
            connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

            return (new RedisTwoFactorSessionStore(connectionMock.Object, Mock.Of<ILogger<RedisTwoFactorSessionStore>>()), dbMock);
        }

        [Fact]
        public async Task SaveAsync_LuegoGetAsync_DevuelveLaSesionCompleta()
        {
            var redis = new Dictionary<string, string>();
            var (store, _) = CrearStore(redis);
            var session = CrearSesion();

            await store.SaveAsync("abc", session, TimeSpan.FromMinutes(5));
            var recuperada = await store.GetAsync("abc");

            Assert.True(redis.ContainsKey($"{KeyPrefix}abc"));
            Assert.NotNull(recuperada);
            Assert.Equal(session.PersonId, recuperada.PersonId);
            Assert.Equal(session.DocumentNumber, recuperada.DocumentNumber);
            Assert.Equal(session.CodeHash, recuperada.CodeHash);
            Assert.Equal(session.Email, recuperada.Email);
            Assert.Equal(session.Attempts, recuperada.Attempts);
        }

        [Fact]
        public async Task SaveAsync_PropagaElTtlRecibido()
        {
            var (store, dbMock) = CrearStore(new Dictionary<string, string>());
            var ttl = TimeSpan.FromMinutes(7);

            await store.SaveAsync("abc", CrearSesion(), ttl);

            dbMock.Verify(d => d.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString() == $"{KeyPrefix}abc"),
                It.IsAny<RedisValue>(),
                ttl,
                It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task GetAsync_SesionInexistente_DevuelveNull()
        {
            var (store, _) = CrearStore(new Dictionary<string, string>());

            Assert.Null(await store.GetAsync("no-existe"));
        }

        [Fact]
        public async Task GetAsync_JsonCorrupto_DevuelveNullYBorraLaClave()
        {
            var redis = new Dictionary<string, string> { [$"{KeyPrefix}abc"] = "{ esto no es json" };
            var (store, dbMock) = CrearStore(redis);

            var recuperada = await store.GetAsync("abc");

            Assert.Null(recuperada);
            Assert.False(redis.ContainsKey($"{KeyPrefix}abc"));
            dbMock.Verify(d => d.KeyDeleteAsync(
                It.Is<RedisKey>(k => k.ToString() == $"{KeyPrefix}abc"), It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task GetAsync_JsonQueDeserializaANull_DevuelveNullYBorraLaClave()
        {
            var redis = new Dictionary<string, string> { [$"{KeyPrefix}abc"] = "null" };
            var (store, dbMock) = CrearStore(redis);

            Assert.Null(await store.GetAsync("abc"));
            dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_SobrescribeLaSesionGuardada()
        {
            var redis = new Dictionary<string, string>();
            var (store, _) = CrearStore(redis);

            await store.SaveAsync("abc", CrearSesion(), TimeSpan.FromMinutes(5));

            var actualizada = CrearSesion();
            actualizada.Attempts = 3;
            await store.UpdateAsync("abc", actualizada, TimeSpan.FromMinutes(5));

            var recuperada = await store.GetAsync("abc");
            Assert.NotNull(recuperada);
            Assert.Equal(3, recuperada.Attempts);
        }

        [Fact]
        public async Task DeleteAsync_EliminaLaClaveConElPrefijo()
        {
            var redis = new Dictionary<string, string>();
            var (store, dbMock) = CrearStore(redis);
            await store.SaveAsync("abc", CrearSesion(), TimeSpan.FromMinutes(5));

            await store.DeleteAsync("abc");

            Assert.False(redis.ContainsKey($"{KeyPrefix}abc"));
            dbMock.Verify(d => d.KeyDeleteAsync(
                It.Is<RedisKey>(k => k.ToString() == $"{KeyPrefix}abc"), It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task GetTtlAsync_DevuelveElTtlDeLaClave()
        {
            var (store, dbMock) = CrearStore(new Dictionary<string, string>());
            dbMock
                .Setup(d => d.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(TimeSpan.FromMinutes(4));

            var ttl = await store.GetTtlAsync("abc");

            Assert.Equal(TimeSpan.FromMinutes(4), ttl);
            dbMock.Verify(d => d.KeyTimeToLiveAsync(
                It.Is<RedisKey>(k => k.ToString() == $"{KeyPrefix}abc"), It.IsAny<CommandFlags>()), Times.Once);
        }
    }
}
