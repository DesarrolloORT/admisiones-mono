using AppLogic.Platform.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Xunit;

namespace UnitTesting.AppLogic.Platform
{
    /// <summary>
    /// Rate limiter del login. Es un control de seguridad (OWASP Anti-Automation) y estaba
    /// prácticamente sin cobertura: <c>AppLogic.Platform</c> tenía 10,6% de líneas cubiertas.
    /// <para>
    /// Varios de estos tests documentan el <b>comportamiento ante fallo de Redis</b>, que hoy es
    /// inconsistente a propósito y está marcado en el propio código como decisión revisable.
    /// Si alguien lo cambia, estos tests se ponen rojos y obligan a que sea una decisión explícita.
    /// </para>
    /// </summary>
    public class RedisRateLimiterServiceTests
    {
        private readonly Mock<IDatabase> _db = new();
        private readonly Mock<ITransaction> _transaction = new();
        private readonly Mock<IConnectionMultiplexer> _redis = new();

        public RedisRateLimiterServiceTests()
        {
            _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_db.Object);
            _db.Setup(d => d.CreateTransaction(It.IsAny<object>())).Returns(_transaction.Object);

            // Las operaciones encoladas en la transacción devuelven tasks que se resuelven al ejecutar.
            _transaction
                .Setup(t => t.SortedSetRemoveRangeByScoreAsync(
                    It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(),
                    It.IsAny<Exclude>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(0);
            _transaction
                .Setup(t => t.SortedSetAddAsync(
                    It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(),
                    It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            _transaction
                .Setup(t => t.KeyExpireAsync(
                    It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
        }

        private RedisRateLimiterService CrearServicio() =>
            new(_redis.Object, NullLogger<RedisRateLimiterService>.Instance);

        private void ConteoEnLaVentana(long cantidad) =>
            _transaction
                .Setup(t => t.SortedSetLengthAsync(
                    It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(),
                    It.IsAny<Exclude>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(cantidad);

        private void TransaccionSeEjecuta(bool exito) =>
            _transaction.Setup(t => t.ExecuteAsync(It.IsAny<CommandFlags>())).ReturnsAsync(exito);

        // ───── Ventana deslizante ────────────────────────────────────────────

        [Theory]
        [InlineData(1, 5, true)]
        [InlineData(5, 5, true)]   // el límite es inclusivo: el intento nº5 pasa
        [InlineData(6, 5, false)]  // el nº6 no
        public async Task IsAllowedAsync_CompararContraElLimiteEsInclusivo(long conteo, int limite, bool esperado)
        {
            ConteoEnLaVentana(conteo);
            TransaccionSeEjecuta(true);

            var permitido = await CrearServicio().IsAllowedAsync("k", limite, TimeSpan.FromMinutes(15));

            Assert.Equal(esperado, permitido);
        }

        [Fact]
        public async Task IsAllowedAsync_LimpiaLosIntentosFueraDeLaVentanaYPoneExpiracion()
        {
            ConteoEnLaVentana(1);
            TransaccionSeEjecuta(true);

            await CrearServicio().IsAllowedAsync("k", 5, TimeSpan.FromMinutes(15));

            // Sin el cleanup la ventana no sería deslizante; sin el expire las claves no se recuperan.
            _transaction.Verify(t => t.SortedSetRemoveRangeByScoreAsync(
                It.IsAny<RedisKey>(), double.NegativeInfinity, It.IsAny<double>(),
                It.IsAny<Exclude>(), It.IsAny<CommandFlags>()), Times.Once);
            _transaction.Verify(t => t.KeyExpireAsync(
                It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Fact]
        public async Task IsAllowedAsync_UsaElPrefijoRatelimitEnLaClave()
        {
            ConteoEnLaVentana(1);
            TransaccionSeEjecuta(true);

            await CrearServicio().IsAllowedAsync("login-ip:1.2.3.4", 5, TimeSpan.FromMinutes(15));

            _transaction.Verify(t => t.KeyExpireAsync(
                (RedisKey)"ratelimit:login-ip:1.2.3.4",
                It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()),
                Times.Once);
        }

        // ───── Comportamiento ante fallo de Redis ────────────────────────────

        [Fact]
        public async Task IsAllowedAsync_SiLaTransaccionNoSeEjecuta_RECHAZA()
        {
            // fail-safe: ante transacción fallida se rechaza el request.
            ConteoEnLaVentana(1);
            TransaccionSeEjecuta(false);

            var permitido = await CrearServicio().IsAllowedAsync("k", 5, TimeSpan.FromMinutes(15));

            Assert.False(permitido);
        }

        [Fact]
        public async Task IsAllowedAsync_SiRedisNoEstaDisponible_PERMITE_failOpen()
        {
            // ⚠️ fail-OPEN, opuesto al caso de arriba: si Redis se cae, el rate limiting del login
            // queda DESACTIVADO y no hay tope de intentos de contraseña.
            // Es una decisión consciente (prioriza disponibilidad) y está anotada en el propio
            // servicio. Este test la fija: si se cambia a fail-closed, hay que cambiarlo acá también.
            _db.Setup(d => d.CreateTransaction(It.IsAny<object>()))
                .Throws(new RedisConnectionException(ConnectionFailureType.SocketFailure, "caído"));

            var permitido = await CrearServicio().IsAllowedAsync("k", 5, TimeSpan.FromMinutes(15));

            Assert.True(permitido);
        }

        [Fact]
        public async Task IsAllowedAsync_AnteCualquierOtraExcepcion_PERMITE_failOpen()
        {
            _db.Setup(d => d.CreateTransaction(It.IsAny<object>()))
                .Throws(new InvalidOperationException("algo raro"));

            var permitido = await CrearServicio().IsAllowedAsync("k", 5, TimeSpan.FromMinutes(15));

            Assert.True(permitido);
        }

        // ───── Restantes ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(0, 5, 5)]
        [InlineData(3, 5, 2)]
        [InlineData(5, 5, 0)]
        [InlineData(9, 5, 0)]   // nunca negativo
        public async Task GetRemainingAsync_NoDevuelveNegativos(long usados, int limite, int esperado)
        {
            _db.Setup(d => d.SortedSetRemoveRangeByScoreAsync(
                    It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(),
                    It.IsAny<Exclude>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(0);
            _db.Setup(d => d.SortedSetLengthAsync(
                    It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(),
                    It.IsAny<Exclude>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(usados);

            var restantes = await CrearServicio().GetRemainingAsync("k", limite, TimeSpan.FromMinutes(15));

            Assert.Equal(esperado, restantes);
        }

        // ───── Clave de partición del login ──────────────────────────────────

        [Fact]
        public async Task ValidateAsync_ArmaLaClaveConDocumentoNormalizadoEIp()
        {
            ConteoEnLaVentana(1);
            TransaccionSeEjecuta(true);
            _db.Setup(d => d.SortedSetLengthAsync(
                    It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(),
                    It.IsAny<Exclude>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(1);

            var result = await CrearServicio().ValidateAsync(
                "1.2.3.4", "CI", "1.234.567-2", 5, TimeSpan.FromMinutes(15));

            // El documento se normaliza para que "1.234.567-2" y "12345672" compartan partición:
            // si no, cambiar el formato del documento sería una forma trivial de evadir el límite.
            Assert.Equal("login-account:CI:12345672:ip:1.2.3.4", result.PartitionKey);
            Assert.True(result.IsAllowed);
        }

        [Theory]
        [InlineData("1.234.567-2")]
        [InlineData("12345672")]
        [InlineData(" 1234567-2 ")]
        public async Task ValidateAsync_DistintosFormatosDelMismoDocumentoCaenEnLaMismaParticion(string documento)
        {
            ConteoEnLaVentana(1);
            TransaccionSeEjecuta(true);
            _db.Setup(d => d.SortedSetLengthAsync(
                    It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(),
                    It.IsAny<Exclude>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(1);

            var result = await CrearServicio().ValidateAsync(
                "1.2.3.4", "CI", documento, 5, TimeSpan.FromMinutes(15));

            Assert.Equal("login-account:CI:12345672:ip:1.2.3.4", result.PartitionKey);
        }
    }
}
