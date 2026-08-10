using AppLogic.Identity.Services;
using AppLogic.Identity.Dtos;
using AppLogic.Registration.Dtos;
using AppLogic.Registration.Services;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace UnitTesting.AppLogic.Services
{
    public class RedisPendingPersonStoreTests
    {
        private static PendingPerson CrearPending(string flowId, string documentType = "CI", string document = "1234567-2")
            => new()
            {
                FlowId = flowId,
                DocumentType = documentType,
                DocumentNumber = document,
                FirstSurname = "Perez",
                FirstName = "Ana",
                BirthDate = new DateTime(1990, 1, 1),
                Sex = "F",
                Address = "Calle 1",
                PrimaryPhone = "+59899123456",
                Email = "ana@example.com",
                TokenHash = "hash",
                CountryId = 1,
                StateId = 1,
                CityId = 1,
                CreatedAt = DateTime.UtcNow
            };

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
                .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Callback<RedisKey, CommandFlags>((key, _) => redis.Remove(key.ToString()))
                .ReturnsAsync(true);
            return redisDbMock;
        }

        private static RedisPendingPersonStore CrearStore(Dictionary<string, string> redis)
        {
            var redisDbMock = CrearRedisMock(redis);
            var connectionMock = new Mock<IConnectionMultiplexer>();
            connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDbMock.Object);
            return new RedisPendingPersonStore(connectionMock.Object);
        }

        [Fact]
        public async Task SaveAsync_ThenGetAsync_RoundTripsPendingPersona()
        {
            var redis = new Dictionary<string, string>();
            var store = CrearStore(redis);
            var pending = CrearPending("flow-1");

            await store.SaveAsync(pending, TimeSpan.FromHours(1));
            var result = await store.GetAsync("flow-1");

            Assert.NotNull(result);
            Assert.Equal("flow-1", result!.FlowId);
            Assert.Equal("ana@example.com", result.Email);
            Assert.Equal("hash", result.TokenHash);
        }

        [Fact]
        public async Task SaveAsync_AlsoWritesDocumentoToFlowIdMapping()
        {
            var redis = new Dictionary<string, string>();
            var store = CrearStore(redis);
            var pending = CrearPending("flow-1", documentType: "CI", document: "1234567-2");

            await store.SaveAsync(pending, TimeSpan.FromHours(1));

            Assert.True(redis.ContainsKey("registro:pending:flow-1"));
            Assert.Equal("flow-1", redis["registro:pending-doc:CI:12345672"]);
        }

        [Fact]
        public async Task GetRawAsync_WhenKeyMissing_ReturnsNull()
        {
            var store = CrearStore([]);

            var result = await store.GetRawAsync("no-existe");

            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_RemovesPendingAndDocumentoMapping()
        {
            var redis = new Dictionary<string, string>();
            var store = CrearStore(redis);
            var pending = CrearPending("flow-1");
            await store.SaveAsync(pending, TimeSpan.FromHours(1));

            await store.DeleteAsync("flow-1");

            Assert.False(redis.ContainsKey("registro:pending:flow-1"));
            Assert.False(redis.ContainsKey("registro:pending-doc:CI:12345672"));
        }

        [Fact]
        public async Task ResolverFlowIdPorDocumentoAsync_WhenNoMappingExists_ReturnsNull()
        {
            var store = CrearStore([]);

            var result = await store.ResolveFlowIdByDocumentAsync("CI", "1234567-2");

            Assert.Null(result);
        }

        [Fact]
        public async Task ResolverFlowIdPorDocumentoAsync_WhenPendingStillExists_ReturnsFlowId()
        {
            var redis = new Dictionary<string, string>();
            var store = CrearStore(redis);
            var pending = CrearPending("flow-1");
            await store.SaveAsync(pending, TimeSpan.FromHours(1));

            var result = await store.ResolveFlowIdByDocumentAsync("CI", "1234567-2");

            Assert.Equal("flow-1", result);
        }

        [Fact]
        public async Task ResolverFlowIdPorDocumentoAsync_WhenMappingIsStale_CleansUpAndReturnsNull()
        {
            var redis = new Dictionary<string, string>
            {
                ["registro:pending-doc:CI:12345672"] = "flow-huerfano"
            };
            var store = CrearStore(redis);

            var result = await store.ResolveFlowIdByDocumentAsync("CI", "1234567-2");

            Assert.Null(result);
            Assert.False(redis.ContainsKey("registro:pending-doc:CI:12345672"));
        }
    }
}
