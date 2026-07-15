using AppLogic.Catalogos.Interfaces;
using AppLogic.Catalogos.Dtos;
using AppLogic.DevartDTOs;
using AppLogic.ApiClients.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using Xunit;
using WebApiAdmisiones.Security.Authentication;

namespace UnitTesting.Controllers
{
    public class CatalogosControllerTests
    {
        private readonly Mock<ICatalogosService> _serviceMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<ILogger<CatalogosController>> _loggerMock;

        public CatalogosControllerTests()
        {
            _serviceMock = new Mock<ICatalogosService>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<CatalogosController>>();
        }

        private CatalogosController CreateController()
        {
            return new CatalogosController(
                _serviceMock.Object,
                _loggerMock.Object,
                _currentUserMock.Object);
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
        public async Task ObtenerPaisesEstadosCiudades_DelegatesToServiceAndReturnsOk()
        {
            // La lógica de cache vive ahora en CatalogosCacheDecorator (ver
            // CatalogosCacheDecoratorTests); el controller solo delega en ICatalogosService.
            var controller = CreateController();
            var expectedData = new List<DtoPaisEstadoCiudadResponse>
            {
                new DtoPaisEstadoCiudadResponse { CodigoPais = 1, Nombre = "Uruguay" }
            };
            _serviceMock
                .Setup(s => s.ObtenerPaisesEstadosCiudadesAsync())
                .ReturnsAsync(OperationResult<IEnumerable<DtoPaisEstadoCiudadResponse>>.Ok(
                    expectedData,
                    nameof(ICatalogosService.ObtenerPaisesEstadosCiudadesAsync)));

            var response = await controller.ObtenerPaisesEstadosCiudades();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _serviceMock.Verify(s => s.ObtenerPaisesEstadosCiudadesAsync(), Times.Once);
        }

        [Fact]
        public void ObtenerEncuestaInicial_ReturnsOk()
        {
            var serviceMock = new Mock<ICatalogosService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<CatalogosController>>();
            var controller = new CatalogosController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            serviceMock.Setup(s => s.ObtenerEncuestaInicial())
                .Returns(OperationResult<DtoEncuestaInicialCatalogosResponse>.Ok(
                    new DtoEncuestaInicialCatalogosResponse
                    {
                        DecisionAcademica = new DtoEncuestaDecisionAcademicaCatalogos
                        {
                            AniosEducacionMediaSuperior =
                            [
                                new DtoComboOption { Value = 2, Label = "1° EMS (4° año)" }
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
            var controller = new CatalogosController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

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
                .Returns(OperationResult<IEnumerable<DtoCarrerasPorNivelResponse>>.Ok(
                    [
                        new DtoCarrerasPorNivelResponse
                        {
                            IdNivelProducto = 1,
                            NombreNivelProducto = "Carrera",
                            Escuelas =
                            [
                                new DtoCarrerasPorEscuelaResponse
                                {
                                    IdEscuela = 10,
                                    NombreEscuela = "Facultad",
                                    Productos = [new DtoCarreraResponse { IdProducto = 10, NombreProducto = "ATI" }]
                                }
                            ]
                        }
                    ],
                    nameof(ICatalogosService.ObtenerCarreras)));

            var response = controller.ObtenerCarreras();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
