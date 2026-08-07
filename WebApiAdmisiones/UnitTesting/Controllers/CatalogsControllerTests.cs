using AppLogic.Contracts.Dtos;
using AppLogic.Catalogs.Interfaces;
using AppLogic.Catalogs.Dtos;
using AppLogic.DevartDTOs;
using AppLogic.Integrations.EnrollmentsAndPayments.Dtos;
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
        private readonly Mock<ICatalogService> _serviceMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<ILogger<CatalogsController>> _loggerMock;

        public CatalogosControllerTests()
        {
            _serviceMock = new Mock<ICatalogService>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<CatalogsController>>();
        }

        private CatalogsController CreateController()
        {
            return new CatalogsController(
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
                .Returns(OperationResult<DtoPaisDevart>.Ok(pais, nameof(ICatalogService.ObtenerPais)));

            var response = controller.ObtenerPais(1);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
        */

        [Fact]
        public void CatalogosController_NoLongerExposesPaises()
        {
            Assert.Null(typeof(CatalogsController).GetMethod("ObtenerPaises"));
        }

        [Fact]
        public async Task ObtenerPaisesEstadosCiudades_DelegatesToServiceAndReturnsOk()
        {
            // La lógica de cache vive ahora en CatalogCacheDecorator (ver
            // CatalogCacheDecoratorTests); el controller solo delega en ICatalogService.
            var controller = CreateController();
            var expectedData = new List<CountryStateCityResponse>
            {
                new CountryStateCityResponse { CountryId = 1, Name = "Uruguay" }
            };
            _serviceMock
                .Setup(s => s.GetCountriesStatesCitiesAsync())
                .ReturnsAsync(OperationResult<IEnumerable<CountryStateCityResponse>>.Ok(
                    expectedData,
                    nameof(ICatalogService.GetCountriesStatesCitiesAsync)));

            var response = await controller.GetCountriesStatesCities();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _serviceMock.Verify(s => s.GetCountriesStatesCitiesAsync(), Times.Once);
        }

        [Fact]
        public async Task ObtenerEncuestaInicial_ReturnsOk()
        {
            var serviceMock = new Mock<ICatalogService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<CatalogsController>>();
            var controller = new CatalogsController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            serviceMock.Setup(s => s.GetInitialSurveyCatalogsAsync())
                .ReturnsAsync(OperationResult<InitialSurveyCatalogsResponse>.Ok(
                    new InitialSurveyCatalogsResponse
                    {
                        AcademicDecision = new SurveyAcademicDecisionCatalogs
                        {
                            UpperSecondaryYears =
                            [
                                new ComboOption { Value = 2, Label = "1° EMS (4° año)" }
                            ]
                        }
                    },
                    nameof(ICatalogService.GetInitialSurveyCatalogsAsync)));

            var response = await controller.GetInitialSurvey();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ObtenerComienzos_ReturnsOk()
        {
            var controller = CreateController();

            _serviceMock.Setup(s => s.GetIntakes(10))
                .Returns(OperationResult<IEnumerable<IntakeResponse>>.Ok(
                    [new IntakeResponse { AdmissionProcessId = 20, AdmissionProcessName = "Marzo" }],
                    nameof(ICatalogService.GetIntakes)));

            var response = controller.GetIntakes(10);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ObtenerTurnos_ReturnsOk()
        {
            var serviceMock = new Mock<ICatalogService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<CatalogsController>>();
            var controller = new CatalogsController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.GetShifts(It.IsAny<long>(), 10, 20))
                .ReturnsAsync(OperationResult<List<OfferingResponse>>.Ok(
                    [
                        new OfferingResponse
                        {
                            OfferingId = 1,
                            Shift = new ShiftResponse { ShiftId = 1, ShiftName = "Matutino" }
                        }
                    ],
                    nameof(ICatalogService.GetShifts)));

            var response = await controller.GetShifts(10, 20);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ObtenerCarreras_ReturnsOk()
        {
            var controller = CreateController();

            _serviceMock.Setup(s => s.GetDegreePrograms(It.IsAny<long>(), It.IsAny<AcademicOffer>()))
                .Returns(OperationResult<IEnumerable<DegreeProgramsByLevelResponse>>.Ok(
                    [
                        new DegreeProgramsByLevelResponse
                        {
                            ProductLevelId = 1,
                            ProductLevelName = "Carrera",
                            Schools =
                            [
                                new DegreeProgramsBySchoolResponse
                                {
                                    SchoolId = 10,
                                    SchoolName = "Facultad",
                                    Products = [new DegreeProgramResponse { ProductId = 10, ProductName = "ATI" }]
                                }
                            ]
                        }
                    ],
                    nameof(ICatalogService.GetDegreePrograms)));

            var response = controller.GetDegreePrograms(AcademicOffer.UniversityDegree);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
