using AppLogic.ApiClients.Dtos;
using AppLogic.Catalogos.Interfaces;
using AppLogic.Catalogos.Dtos;
using AppLogic.DevartDTOs;
using Microsoft.Extensions.Configuration;
using Moq;
using Utilities;
using WebApiAdmisiones.Security.Cache;
using Xunit;

namespace UnitTesting.Security.Cache
{
    public class CatalogosCacheDecoratorTests
    {
        private const string CacheKey = "catalogos:paises-estados-ciudades";

        private readonly Mock<ICatalogosService> _innerMock = new();
        private readonly Mock<IRedisCacheService> _cacheMock = new();

        private static IConfiguration CrearConfiguracion(int? ttlHours = null)
            => new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cache:CatalogosTTLHours"] = ttlHours?.ToString()
                })
                .Build();

        private static List<DtoPaisEstadoCiudadResponse> CrearData()
            => [new DtoPaisEstadoCiudadResponse { CodigoPais = 1, Nombre = "Uruguay" }];

        private CatalogosCacheDecorator CrearDecorator(int? ttlHours = null)
            => new(_innerMock.Object, _cacheMock.Object, CrearConfiguracion(ttlHours));

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_CacheHit_ReturnsCachedDataWithoutCallingInner()
        {
            var expectedData = CrearData();
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    CacheKey,
                    It.IsAny<Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(expectedData);
            var decorator = CrearDecorator(24);

            var result = await decorator.ObtenerPaisesEstadosCiudadesAsync();

            Assert.True(result.Success);
            Assert.Same(expectedData, result.Data);
            Assert.Equal("ObtenerPaisesEstadosCiudades", result.Method);
            _innerMock.Verify(s => s.ObtenerPaisesEstadosCiudadesAsync(), Times.Never);
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_CacheMissWithServiceSuccess_CallsInnerOnceViaFactory()
        {
            var expectedData = CrearData();
            _innerMock
                .Setup(s => s.ObtenerPaisesEstadosCiudadesAsync())
                .ReturnsAsync(OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>.Ok(
                    expectedData,
                    "ObtenerPaisesEstadosCiudades"));

            // Simula un cache MISS real: GetOrSetAsync ejecuta la factory internamente
            // (igual que hace RedisCacheService.GetOrSetAsync en cache miss).
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    CacheKey,
                    It.IsAny<Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>, TimeSpan>(
                    (_, factory, _) => factory());
            var decorator = CrearDecorator(24);

            var result = await decorator.ObtenerPaisesEstadosCiudadesAsync();

            Assert.True(result.Success);
            Assert.Equal(expectedData, result.Data);
            Assert.Equal("ObtenerPaisesEstadosCiudades", result.Method);
            _innerMock.Verify(s => s.ObtenerPaisesEstadosCiudadesAsync(), Times.Once);
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_CacheMissWithServiceFailure_FallsBackToInnerAgain()
        {
            var fallbackResult = OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>.IsFailed(
                "CAT_01",
                "ObtenerPaisesEstadosCiudades",
                "Error de catálogo.",
                400,
                default!);
            _innerMock
                .Setup(s => s.ObtenerPaisesEstadosCiudadesAsync())
                .ReturnsAsync(fallbackResult);
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    CacheKey,
                    It.IsAny<Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>, TimeSpan>(
                    (_, factory, _) => factory());
            var decorator = CrearDecorator(24);

            var result = await decorator.ObtenerPaisesEstadosCiudadesAsync();

            Assert.False(result.Success);
            Assert.Equal("CAT_01", result.ErrorCode);
            // 1 vez dentro de la factory (para intentar poblar cache) + 1 vez de fallback,
            // porque la factory devolvió null (Success=false) y GetOrSetAsync devolvió null.
            _innerMock.Verify(s => s.ObtenerPaisesEstadosCiudadesAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_UsesConfiguredTtl()
        {
            TimeSpan? capturedTtl = null;
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Callback<string, Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>, TimeSpan>(
                    (_, _, ttl) => capturedTtl = ttl)
                .ReturnsAsync(CrearData());
            var decorator = CrearDecorator(6);

            await decorator.ObtenerPaisesEstadosCiudadesAsync();

            Assert.Equal(TimeSpan.FromHours(6), capturedTtl);
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_WithoutConfiguredTtl_UsesDefault24Hours()
        {
            TimeSpan? capturedTtl = null;
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Callback<string, Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>, TimeSpan>(
                    (_, _, ttl) => capturedTtl = ttl)
                .ReturnsAsync(CrearData());
            var decorator = CrearDecorator();

            await decorator.ObtenerPaisesEstadosCiudadesAsync();

            Assert.Equal(TimeSpan.FromHours(24), capturedTtl);
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_UsesExpectedCacheKey()
        {
            string? capturedKey = null;
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Callback<string, Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>, TimeSpan>(
                    (key, _, _) => capturedKey = key)
                .ReturnsAsync(CrearData());
            var decorator = CrearDecorator(24);

            await decorator.ObtenerPaisesEstadosCiudadesAsync();

            Assert.Equal(CacheKey, capturedKey);
        }

        [Fact]
        public void ObtenerEncuestaInicial_DelegatesToInnerService()
        {
            _innerMock
                .Setup(s => s.ObtenerEncuestaInicial())
                .Returns(OperationResult<DtoEncuestaInicialCatalogosResponse>.Ok(
                    new DtoEncuestaInicialCatalogosResponse(),
                    nameof(ICatalogosService.ObtenerEncuestaInicial)));
            var decorator = CrearDecorator(24);

            var result = decorator.ObtenerEncuestaInicial();

            Assert.True(result.Success);
            _innerMock.Verify(s => s.ObtenerEncuestaInicial(), Times.Once);
        }

        [Fact]
        public void ObtenerCarreras_DelegatesToInnerService()
        {
            _innerMock
                .Setup(s => s.ObtenerCarreras(123, PropuestaAcademica.CarreraUniversitaria))
                .Returns(OperationResult<IEnumerable<DtoCarrerasPorNivelResponse>>.Ok(
                    [],
                    nameof(ICatalogosService.ObtenerCarreras)));
            var decorator = CrearDecorator(24);

            var result = decorator.ObtenerCarreras(123, PropuestaAcademica.CarreraUniversitaria);

            Assert.True(result.Success);
            _innerMock.Verify(s => s.ObtenerCarreras(123, PropuestaAcademica.CarreraUniversitaria), Times.Once);
        }

        [Fact]
        public void ObtenerComienzos_DelegatesToInnerService()
        {
            _innerMock
                .Setup(s => s.ObtenerComienzos(10))
                .Returns(OperationResult<IEnumerable<DtoComienzoResponse>>.Ok(
                    [],
                    nameof(ICatalogosService.ObtenerComienzos)));
            var decorator = CrearDecorator(24);

            var result = decorator.ObtenerComienzos(10);

            Assert.True(result.Success);
            _innerMock.Verify(s => s.ObtenerComienzos(10), Times.Once);
        }

        [Fact]
        public async Task ObtenerTurnos_DelegatesToInnerService()
        {
            _innerMock
                .Setup(s => s.ObtenerTurnos(10, 20))
                .ReturnsAsync(OperationResult<List<OfertaInscripcionDto>>.Ok(
                    [],
                    nameof(ICatalogosService.ObtenerTurnos)));
            var decorator = CrearDecorator(24);

            var result = await decorator.ObtenerTurnos(10, 20);

            Assert.True(result.Success);
            _innerMock.Verify(s => s.ObtenerTurnos(10, 20), Times.Once);
        }

        [Fact]
        public void ObtenerBancos_DelegatesToInnerService()
        {
            _innerMock
                .Setup(s => s.ObtenerBancos())
                .Returns(OperationResult<IEnumerable<DtoBancoDevart>>.Ok(
                    [],
                    nameof(ICatalogosService.ObtenerBancos)));
            var decorator = CrearDecorator(24);

            var result = decorator.ObtenerBancos();

            Assert.True(result.Success);
            _innerMock.Verify(s => s.ObtenerBancos(), Times.Once);
        }

        [Fact]
        public void ObtenerInstituciones_DelegatesToInnerService()
        {
            _innerMock
                .Setup(s => s.ObtenerInstituciones(1, 2))
                .Returns(OperationResult<IEnumerable<DtoEmpresaDevart>>.Ok(
                    [],
                    nameof(ICatalogosService.ObtenerInstituciones)));
            var decorator = CrearDecorator(24);

            var result = decorator.ObtenerInstituciones(1, 2);

            Assert.True(result.Success);
            _innerMock.Verify(s => s.ObtenerInstituciones(1, 2), Times.Once);
        }

        [Fact]
        public void ObtenerFondosDeBecaPorProducto_DelegatesToInnerService()
        {
            _innerMock
                .Setup(s => s.ObtenerFondosDeBecaPorProducto(5))
                .Returns(OperationResult<IEnumerable<DtoTipoDescuentoDevart>>.Ok(
                    [],
                    nameof(ICatalogosService.ObtenerFondosDeBecaPorProducto)));
            var decorator = CrearDecorator(24);

            var result = decorator.ObtenerFondosDeBecaPorProducto(5);

            Assert.True(result.Success);
            _innerMock.Verify(s => s.ObtenerFondosDeBecaPorProducto(5), Times.Once);
        }
    }
}
