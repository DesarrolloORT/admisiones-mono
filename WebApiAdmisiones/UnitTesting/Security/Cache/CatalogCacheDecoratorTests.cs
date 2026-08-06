using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
using AppLogic.Catalogs.Interfaces;
using AppLogic.Catalogs.Dtos;
using AppLogic.DevartDTOs;
using Microsoft.Extensions.Configuration;
using Moq;
using Utilities;
using WebApiAdmisiones.Security.Cache;
using Xunit;

namespace UnitTesting.Security.Cache
{
    public class CatalogCacheDecoratorTests
    {
        private const string CacheKey = "catalogos:paises-estados-ciudades";
        private const string EncuestaInicialCacheKey = "catalogos:encuesta-inicial";
        private const string BancosCacheKey = "catalogos:bancos";

        private readonly Mock<ICatalogService> _innerMock = new();
        private readonly Mock<IRedisCacheService> _cacheMock = new();

        private static IConfiguration CrearConfiguracion(int? ttlHours = null)
            => new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cache:CatalogosTTLHours"] = ttlHours?.ToString()
                })
                .Build();

        private static List<CountryStateCityResponse> CrearData()
            => [new CountryStateCityResponse { CountryId = 1, Name = "Uruguay" }];

        private static InitialSurveyCatalogsResponse CrearEncuestaInicialData()
            => new();

        private static List<BankResponse> CrearBancosData()
            => [new BankResponse { Id = 1, Name = "BROU", Code = 1 }];

        private CatalogCacheDecorator CrearDecorator(int? ttlHours = null)
            => new(_innerMock.Object, _cacheMock.Object, CrearConfiguracion(ttlHours));

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_CacheHit_ReturnsCachedDataWithoutCallingInner()
        {
            var expectedData = CrearData();
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    CacheKey,
                    It.IsAny<Func<Task<IEnumerable<CountryStateCityResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(expectedData);
            var decorator = CrearDecorator(24);

            var result = await decorator.GetCountriesStatesCitiesAsync();

            Assert.True(result.Success);
            Assert.Same(expectedData, result.Data);
            Assert.Equal("GetCountriesStatesCities", result.Method);
            _innerMock.Verify(s => s.GetCountriesStatesCitiesAsync(), Times.Never);
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_CacheMissWithServiceSuccess_CallsInnerOnceViaFactory()
        {
            var expectedData = CrearData();
            _innerMock
                .Setup(s => s.GetCountriesStatesCitiesAsync())
                .ReturnsAsync(OperationResult<IEnumerable<CountryStateCityResponse>>.Ok(
                    expectedData,
                    "GetCountriesStatesCities"));

            // Simula un cache MISS real: GetOrSetAsync ejecuta la factory internamente
            // (igual que hace RedisCacheService.GetOrSetAsync en cache miss).
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    CacheKey,
                    It.IsAny<Func<Task<IEnumerable<CountryStateCityResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<IEnumerable<CountryStateCityResponse>?>>, TimeSpan>(
                    (_, factory, _) => factory());
            var decorator = CrearDecorator(24);

            var result = await decorator.GetCountriesStatesCitiesAsync();

            Assert.True(result.Success);
            Assert.Equal(expectedData, result.Data);
            Assert.Equal("GetCountriesStatesCities", result.Method);
            _innerMock.Verify(s => s.GetCountriesStatesCitiesAsync(), Times.Once);
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_CacheMissWithServiceFailure_FallsBackToInnerAgain()
        {
            var fallbackResult = OperationResult<IEnumerable<CountryStateCityResponse>>.IsFailed(
                "CAT_01",
                "GetCountriesStatesCities",
                "Error de catálogo.",
                400,
                default!);
            _innerMock
                .Setup(s => s.GetCountriesStatesCitiesAsync())
                .ReturnsAsync(fallbackResult);
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    CacheKey,
                    It.IsAny<Func<Task<IEnumerable<CountryStateCityResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<IEnumerable<CountryStateCityResponse>?>>, TimeSpan>(
                    (_, factory, _) => factory());
            var decorator = CrearDecorator(24);

            var result = await decorator.GetCountriesStatesCitiesAsync();

            Assert.False(result.Success);
            Assert.Equal("CAT_01", result.ErrorCode);
            // 1 vez dentro de la factory (para intentar poblar cache) + 1 vez de fallback,
            // porque la factory devolvió null (Success=false) y GetOrSetAsync devolvió null.
            _innerMock.Verify(s => s.GetCountriesStatesCitiesAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_UsesConfiguredTtl()
        {
            TimeSpan? capturedTtl = null;
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<IEnumerable<CountryStateCityResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Callback<string, Func<Task<IEnumerable<CountryStateCityResponse>?>>, TimeSpan>(
                    (_, _, ttl) => capturedTtl = ttl)
                .ReturnsAsync(CrearData());
            var decorator = CrearDecorator(6);

            await decorator.GetCountriesStatesCitiesAsync();

            Assert.Equal(TimeSpan.FromHours(6), capturedTtl);
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_WithoutConfiguredTtl_UsesDefault24Hours()
        {
            TimeSpan? capturedTtl = null;
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<IEnumerable<CountryStateCityResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Callback<string, Func<Task<IEnumerable<CountryStateCityResponse>?>>, TimeSpan>(
                    (_, _, ttl) => capturedTtl = ttl)
                .ReturnsAsync(CrearData());
            var decorator = CrearDecorator();

            await decorator.GetCountriesStatesCitiesAsync();

            Assert.Equal(TimeSpan.FromHours(24), capturedTtl);
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudadesAsync_UsesExpectedCacheKey()
        {
            string? capturedKey = null;
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<IEnumerable<CountryStateCityResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Callback<string, Func<Task<IEnumerable<CountryStateCityResponse>?>>, TimeSpan>(
                    (key, _, _) => capturedKey = key)
                .ReturnsAsync(CrearData());
            var decorator = CrearDecorator(24);

            await decorator.GetCountriesStatesCitiesAsync();

            Assert.Equal(CacheKey, capturedKey);
        }

        [Fact]
        public async Task ObtenerEncuestaInicialAsync_CacheHit_ReturnsCachedDataWithoutCallingInner()
        {
            var expectedData = CrearEncuestaInicialData();
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    EncuestaInicialCacheKey,
                    It.IsAny<Func<Task<InitialSurveyCatalogsResponse?>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(expectedData);
            var decorator = CrearDecorator(24);

            var result = await decorator.GetInitialSurveyCatalogsAsync();

            Assert.True(result.Success);
            Assert.Same(expectedData, result.Data);
            Assert.Equal("GetInitialSurvey", result.Method);
            _innerMock.Verify(s => s.GetInitialSurveyCatalogsAsync(), Times.Never);
        }

        [Fact]
        public async Task ObtenerEncuestaInicialAsync_CacheMissWithServiceSuccess_CallsInnerOnceViaFactory()
        {
            var expectedData = CrearEncuestaInicialData();
            _innerMock
                .Setup(s => s.GetInitialSurveyCatalogsAsync())
                .ReturnsAsync(OperationResult<InitialSurveyCatalogsResponse>.Ok(
                    expectedData,
                    "GetInitialSurvey"));
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    EncuestaInicialCacheKey,
                    It.IsAny<Func<Task<InitialSurveyCatalogsResponse?>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<InitialSurveyCatalogsResponse?>>, TimeSpan>(
                    (_, factory, _) => factory());
            var decorator = CrearDecorator(24);

            var result = await decorator.GetInitialSurveyCatalogsAsync();

            Assert.True(result.Success);
            Assert.Same(expectedData, result.Data);
            _innerMock.Verify(s => s.GetInitialSurveyCatalogsAsync(), Times.Once);
        }

        [Fact]
        public async Task ObtenerEncuestaInicialAsync_CacheMissWithServiceFailure_FallsBackToInnerAgain()
        {
            var fallbackResult = OperationResult<InitialSurveyCatalogsResponse>.IsFailed(
                "CAT_01",
                "GetInitialSurvey",
                "Error de catálogo.",
                400,
                default!);
            _innerMock
                .Setup(s => s.GetInitialSurveyCatalogsAsync())
                .ReturnsAsync(fallbackResult);
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    EncuestaInicialCacheKey,
                    It.IsAny<Func<Task<InitialSurveyCatalogsResponse?>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<InitialSurveyCatalogsResponse?>>, TimeSpan>(
                    (_, factory, _) => factory());
            var decorator = CrearDecorator(24);

            var result = await decorator.GetInitialSurveyCatalogsAsync();

            Assert.False(result.Success);
            Assert.Equal("CAT_01", result.ErrorCode);
            _innerMock.Verify(s => s.GetInitialSurveyCatalogsAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task ObtenerEncuestaInicialAsync_UsesExpectedCacheKey()
        {
            string? capturedKey = null;
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<InitialSurveyCatalogsResponse?>>>(),
                    It.IsAny<TimeSpan>()))
                .Callback<string, Func<Task<InitialSurveyCatalogsResponse?>>, TimeSpan>(
                    (key, _, _) => capturedKey = key)
                .ReturnsAsync(CrearEncuestaInicialData());
            var decorator = CrearDecorator(24);

            await decorator.GetInitialSurveyCatalogsAsync();

            Assert.Equal(EncuestaInicialCacheKey, capturedKey);
        }

        [Fact]
        public void ObtenerCarreras_DelegatesToInnerService()
        {
            _innerMock
                .Setup(s => s.GetDegreePrograms(123, AcademicOffer.UniversityDegree))
                .Returns(OperationResult<IEnumerable<DegreeProgramsByLevelResponse>>.Ok(
                    [],
                    nameof(ICatalogService.GetDegreePrograms)));
            var decorator = CrearDecorator(24);

            var result = decorator.GetDegreePrograms(123, AcademicOffer.UniversityDegree);

            Assert.True(result.Success);
            _innerMock.Verify(s => s.GetDegreePrograms(123, AcademicOffer.UniversityDegree), Times.Once);
        }

        [Fact]
        public void ObtenerComienzos_DelegatesToInnerService()
        {
            _innerMock
                .Setup(s => s.GetIntakes(10))
                .Returns(OperationResult<IEnumerable<IntakeResponse>>.Ok(
                    [],
                    nameof(ICatalogService.GetIntakes)));
            var decorator = CrearDecorator(24);

            var result = decorator.GetIntakes(10);

            Assert.True(result.Success);
            _innerMock.Verify(s => s.GetIntakes(10), Times.Once);
        }

        [Fact]
        public async Task ObtenerTurnos_DelegatesToInnerService()
        {
            _innerMock
                .Setup(s => s.GetShifts(10, 20))
                .ReturnsAsync(OperationResult<List<OfferingResponse>>.Ok(
                    [],
                    nameof(ICatalogService.GetShifts)));
            var decorator = CrearDecorator(24);

            var result = await decorator.GetShifts(10, 20);

            Assert.True(result.Success);
            _innerMock.Verify(s => s.GetShifts(10, 20), Times.Once);
        }

        [Fact]
        public async Task ObtenerBancosAsync_CacheHit_ReturnsCachedDataWithoutCallingInner()
        {
            var expectedData = CrearBancosData();
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    BancosCacheKey,
                    It.IsAny<Func<Task<IEnumerable<BankResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(expectedData);
            var decorator = CrearDecorator(24);

            var result = await decorator.GetBanksAsync();

            Assert.True(result.Success);
            Assert.Same(expectedData, result.Data);
            Assert.Equal("GetBanks", result.Method);
            _innerMock.Verify(s => s.GetBanksAsync(), Times.Never);
        }

        [Fact]
        public async Task ObtenerBancosAsync_CacheMissWithServiceSuccess_CallsInnerOnceViaFactory()
        {
            var expectedData = CrearBancosData();
            _innerMock
                .Setup(s => s.GetBanksAsync())
                .ReturnsAsync(OperationResult<IEnumerable<BankResponse>>.Ok(
                    expectedData,
                    "GetBanks"));
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    BancosCacheKey,
                    It.IsAny<Func<Task<IEnumerable<BankResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<IEnumerable<BankResponse>?>>, TimeSpan>(
                    (_, factory, _) => factory());
            var decorator = CrearDecorator(24);

            var result = await decorator.GetBanksAsync();

            Assert.True(result.Success);
            Assert.Equal(expectedData, result.Data);
            _innerMock.Verify(s => s.GetBanksAsync(), Times.Once);
        }

        [Fact]
        public async Task ObtenerBancosAsync_CacheMissWithServiceFailure_FallsBackToInnerAgain()
        {
            var fallbackResult = OperationResult<IEnumerable<BankResponse>>.IsFailed(
                "CAT_02",
                "GetBanks",
                "Error de catálogo.",
                400,
                default!);
            _innerMock
                .Setup(s => s.GetBanksAsync())
                .ReturnsAsync(fallbackResult);
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    BancosCacheKey,
                    It.IsAny<Func<Task<IEnumerable<BankResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Returns<string, Func<Task<IEnumerable<BankResponse>?>>, TimeSpan>(
                    (_, factory, _) => factory());
            var decorator = CrearDecorator(24);

            var result = await decorator.GetBanksAsync();

            Assert.False(result.Success);
            Assert.Equal("CAT_02", result.ErrorCode);
            _innerMock.Verify(s => s.GetBanksAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task ObtenerBancosAsync_UsesExpectedCacheKey()
        {
            string? capturedKey = null;
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<IEnumerable<BankResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .Callback<string, Func<Task<IEnumerable<BankResponse>?>>, TimeSpan>(
                    (key, _, _) => capturedKey = key)
                .ReturnsAsync(CrearBancosData());
            var decorator = CrearDecorator(24);

            await decorator.GetBanksAsync();

            Assert.Equal(BancosCacheKey, capturedKey);
        }

        [Fact]
        public void ObtenerInstituciones_DelegatesToInnerService()
        {
            _innerMock
                .Setup(s => s.GetInstitutions(1, 2))
                .Returns(OperationResult<IEnumerable<InstitutionResponse>>.Ok(
                    [],
                    nameof(ICatalogService.GetInstitutions)));
            var decorator = CrearDecorator(24);

            var result = decorator.GetInstitutions(1, 2);

            Assert.True(result.Success);
            _innerMock.Verify(s => s.GetInstitutions(1, 2), Times.Once);
        }

    }
}
