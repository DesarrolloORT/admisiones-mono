using AppLogic.Dtos.Catalogos;
using AppLogic.DevartDTOs;
using AppLogic.ApiClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using Xunit;
using WebApiAdmisiones.Security.Authentication;
using WebApiAdmisiones.Security.Cache;
using AppLogic.IServices.Catalogos;

namespace UnitTesting.Controllers
{
    public class CatalogosControllerTests
    {
        private readonly Mock<ICatalogosService> _serviceMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<ILogger<CatalogosController>> _loggerMock;
        private readonly Mock<IRedisCacheService> _cacheMock;
        private readonly IConfiguration _configuration;

        public CatalogosControllerTests()
        {
            _serviceMock = new Mock<ICatalogosService>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<CatalogosController>>();
            _cacheMock = new Mock<IRedisCacheService>();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cache:CatalogosTTLHours"] = "24"
                })
                .Build();
        }

        private CatalogosController CreateController()
        {
            return new CatalogosController(
                _serviceMock.Object,
                _loggerMock.Object,
                _currentUserMock.Object,
                _cacheMock.Object,
                _configuration);
        }

        /*
        [Fact]
        public void ObtenerPais_ReturnsOk()
        {
            var controller = CreateController();
            var pais = new DtoPaisDevart { CodigoPais = 1, Nombre = "Uruguay" };

            _serviceMock.Setup(s => s.ObtenerPais(1))
                .Returns(OperationResult<DtoPaisDevart>.Ok(pais, nameof(ICatalogosService.ObtenerPais)));

            var response = controller.ObtenerPais(1);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
        */

        [Fact]
        public void CatalogosController_NoLongerExposesPaises()
        {
            Assert.Null(typeof(CatalogosController).GetMethod("ObtenerPaises"));
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudades_WithCache_ReturnsOk()
        {
            // Arrange
            var controller = CreateController();
            var expectedData = new List<DtoPaisEstadoCiudadResponse>
            {
                new DtoPaisEstadoCiudadResponse { CodigoPais = 1, Nombre = "Uruguay" }
            };

            // Mock cache devuelve datos (cache HIT)
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    "catalogos:paises-estados-ciudades",
                    It.IsAny<Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(expectedData);

            // Act
            var response = await controller.ObtenerPaisesEstadosCiudades();

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);

            // Verificar que NO se llamÃ³ al servicio (porque cache devolviÃ³ datos)
            _serviceMock.Verify(
                s => s.ObtenerPaisesEstadosCiudadesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudades_CacheMiss_CallsService()
        {
            // Arrange
            var controller = CreateController();
            var expectedData = new List<DtoPaisEstadoCiudadResponse>
            {
                new DtoPaisEstadoCiudadResponse { CodigoPais = 1, Nombre = "Uruguay" }
            };

            // Mock cache devuelve null (cache MISS, luego ejecuta factory internamente)
            _cacheMock
                .Setup(c => c.GetOrSetAsync(
                    "catalogos:paises-estados-ciudades",
                    It.IsAny<Func<Task<IEnumerable<DtoPaisEstadoCiudadResponse>?>>>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync((IEnumerable<DtoPaisEstadoCiudadResponse>?)null);

            // Mock servicio para el fallback
            _serviceMock
                .Setup(s => s.ObtenerPaisesEstadosCiudadesAsync())
                .ReturnsAsync(OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>.Ok(
                    expectedData,
                    nameof(ICatalogosService.ObtenerPaisesEstadosCiudades)));

            // Act
            var response = await controller.ObtenerPaisesEstadosCiudades();

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);

            // Verificar que SÃ se llamÃ³ al servicio (fallback porque cache devolviÃ³ null)
            _serviceMock.Verify(
                s => s.ObtenerPaisesEstadosCiudadesAsync(),
                Times.Once);
        }

        [Fact]
        public void ObtenerEncuestaInicial_ReturnsOk()
        {
            var serviceMock = new Mock<ICatalogosService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<CatalogosController>>();
            var controller = new CatalogosController(serviceMock.Object, loggerMock.Object, currentUserMock.Object, Mock.Of<IRedisCacheService>(), Mock.Of<IConfiguration>());
            serviceMock.Setup(s => s.ObtenerEncuestaInicial())
                .Returns(OperationResult<DtoEncuestaInicialCatalogosResponse>.Ok(
                    new DtoEncuestaInicialCatalogosResponse
                    {
                        DecisionAcademica = new DtoEncuestaDecisionAcademicaCatalogos
                        {
                            AniosEducacionMediaSuperior =
                            [
                                new DtoComboOption { Value = 2, Label = "1\u00b0 EMS (4\u00b0 a\u00f1o)" }
                            ]
                        }
                    },
                    nameof(ICatalogosService.ObtenerEncuestaInicial)));

            var response = controller.ObtenerEncuestaInicial();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ObtenerComienzos_ReturnsOk()
        {
            var controller = CreateController();

            _serviceMock.Setup(s => s.ObtenerComienzos(10))
                .Returns(OperationResult<IEnumerable<DtoComienzoResponse>>.Ok(
                    [new DtoComienzoResponse { IdProceso = 20, NombreProceso = "Marzo" }],
                    nameof(ICatalogosService.ObtenerComienzos)));

            var response = controller.ObtenerComienzos(10);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ObtenerTurnos_ReturnsOk()
        {
            var serviceMock = new Mock<ICatalogosService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<CatalogosController>>();
            var controller = new CatalogosController(serviceMock.Object, loggerMock.Object, currentUserMock.Object, Mock.Of<IRedisCacheService>(), Mock.Of<IConfiguration>());

            serviceMock.Setup(s => s.ObtenerTurnos(10, 20))
                .ReturnsAsync(OperationResult<List<OfertaInscripcionDto>>.Ok(
                    [
                        new OfertaInscripcionDto
                        {
                            IdOferta = 1,
                            Turno = new DtoTurno { IdTurno = 1, NombreTurno = "Matutino" }
                        }
                    ],
                    nameof(ICatalogosService.ObtenerTurnos)));

            var response = await controller.ObtenerTurnos(10, 20);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ObtenerCarreras_ReturnsOk()
        {
            var controller = CreateController();

            _serviceMock.Setup(s => s.ObtenerCarreras(It.IsAny<long>()))
                .Returns(OperationResult<IEnumerable<DtoCarreraResponse>>.Ok(
                    [new DtoCarreraResponse { IdProducto = 10, NombreProducto = "ATI" }],
                    nameof(ICatalogosService.ObtenerCarreras)));

            var response = controller.ObtenerCarreras();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
