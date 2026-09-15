using AppLogic.Platform.RateLimiting;
using Moq;
using WebApiAdmisiones.Security.RateLimiting;
using Xunit;

namespace UnitTesting.Security
{
    /// <summary>
    /// Adaptador entre el rate limiter de ASP.NET Core y el backend Redis: sólo traduce
    /// permisos y respuestas, la ventana la resuelve IRateLimiterService.
    /// </summary>
    public class RedisRateLimiterTests
    {
        private const string Key = "login-ip:127.0.0.1";
        private const int Limit = 10;
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

        private static RedisRateLimiter Crear(Mock<IRateLimiterService> serviceMock)
            => new(serviceMock.Object, Key, Limit, Window);

        private static Mock<IRateLimiterService> ServiceMock(bool allowed)
        {
            var mock = new Mock<IRateLimiterService>();
            mock.Setup(s => s.IsAllowedAsync(Key, Limit, Window)).ReturnsAsync(allowed);
            return mock;
        }

        [Fact]
        public void Constructor_ServicioNull_LanzaArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RedisRateLimiter(null!, Key, Limit, Window));
        }

        [Fact]
        public void Constructor_KeyNull_LanzaArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new RedisRateLimiter(Mock.Of<IRateLimiterService>(), null!, Limit, Window));
        }

        [Fact]
        public void IdleDuration_SiempreEsNull_PorqueElEstadoViveEnRedis()
        {
            using var limiter = Crear(ServiceMock(true));

            Assert.Null(limiter.IdleDuration);
        }

        [Fact]
        public void GetStatistics_DevuelveNull_RedisNoAgregaEstadisticasEnMemoria()
        {
            using var limiter = Crear(ServiceMock(true));

            Assert.Null(limiter.GetStatistics());
        }

        [Fact]
        public async Task AcquireAsync_ServicioPermite_DevuelveLeaseAdquirido()
        {
            var serviceMock = ServiceMock(allowed: true);
            using var limiter = Crear(serviceMock);

            using var lease = await limiter.AcquireAsync(1);

            Assert.True(lease.IsAcquired);
            serviceMock.Verify(s => s.IsAllowedAsync(Key, Limit, Window), Times.Once);
        }

        [Fact]
        public async Task AcquireAsync_ServicioRechaza_DevuelveLeaseNoAdquirido()
        {
            using var limiter = Crear(ServiceMock(allowed: false));

            using var lease = await limiter.AcquireAsync(1);

            Assert.False(lease.IsAcquired);
        }

        [Fact]
        public async Task AcquireAsync_MasDeUnPermit_RechazaSinConsultarRedis()
        {
            var serviceMock = ServiceMock(allowed: true);
            using var limiter = Crear(serviceMock);

            using var lease = await limiter.AcquireAsync(2);

            Assert.False(lease.IsAcquired);
            serviceMock.Verify(s => s.IsAllowedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public void AttemptAcquire_ServicioPermite_DevuelveLeaseAdquirido()
        {
            using var limiter = Crear(ServiceMock(allowed: true));

            using var lease = limiter.AttemptAcquire(1);

            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public void AttemptAcquire_MasDeUnPermit_RechazaSinConsultarRedis()
        {
            var serviceMock = ServiceMock(allowed: true);
            using var limiter = Crear(serviceMock);

            using var lease = limiter.AttemptAcquire(3);

            Assert.False(lease.IsAcquired);
            serviceMock.Verify(s => s.IsAllowedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task Lease_NoExponeMetadata()
        {
            using var limiter = Crear(ServiceMock(allowed: true));

            using var lease = await limiter.AcquireAsync(1);

            Assert.Empty(lease.MetadataNames);
            Assert.False(lease.TryGetMetadata("cualquiera", out var metadata));
            Assert.Null(metadata);
        }

        [Fact]
        public async Task DisposeAsync_NoLanza_YNoDisponeElServicioCompartido()
        {
            var serviceMock = ServiceMock(allowed: true);
            var limiter = Crear(serviceMock);

            await limiter.DisposeAsync();

            serviceMock.VerifyNoOtherCalls();
        }
    }
}
