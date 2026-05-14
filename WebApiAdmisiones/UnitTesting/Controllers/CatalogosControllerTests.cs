using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Controllers
{
    public class CatalogosControllerTests
    {
        [Fact]
        public void ObtenerPais_ReturnsOk()
        {
            var serviceMock = new Mock<ICatalogosService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<CatalogosController>>();
            var controller = new CatalogosController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            var pais = new DtoPaisDevart { CodigoPais = 1, Nombre = "Uruguay" };

            serviceMock.Setup(s => s.ObtenerPais(1))
                .Returns(OperationResult<DtoPaisDevart>.Ok(pais, nameof(ICatalogosService.ObtenerPais)));

            var response = controller.ObtenerPais(1);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void CatalogosController_NoLongerExposesPaises()
        {
            Assert.Null(typeof(CatalogosController).GetMethod("ObtenerPaises"));
        }

        [Fact]
        public void ObtenerPaisesEstadosCiudades_ReturnsOk()
        {
            var serviceMock = new Mock<ICatalogosService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<CatalogosController>>();
            var controller = new CatalogosController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerPaisesEstadosCiudades())
                .Returns(OperationResult<IEnumerable<DtoPaisDevart>>.Ok(
                    [new DtoPaisDevart { CodigoPais = 1, Nombre = "Uruguay" }],
                    nameof(ICatalogosService.ObtenerPaisesEstadosCiudades)));

            var response = controller.ObtenerPaisesEstadosCiudades();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void CatalogosController_ExposesTipoDocumentos()
        {
            Assert.NotNull(typeof(CatalogosController).GetMethod("ObtenerTipoDocumentos"));
        }

        [Fact]
        public void ObtenerTipoDocumentos_ReturnsOk()
        {
            var serviceMock = new Mock<ICatalogosService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<CatalogosController>>();
            var controller = new CatalogosController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerTipoDocumentos())
                .Returns(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>.Ok(
                    [new DtoAcaTipoDocumentoDevart { CodTipoDocumento = 1, Descripcion = "Cedula" }],
                    nameof(ICatalogosService.ObtenerTipoDocumentos)));

            var response = controller.ObtenerTipoDocumentos();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ObtenerComienzos_ReturnsOk()
        {
            var serviceMock = new Mock<ICatalogosService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<CatalogosController>>();
            var controller = new CatalogosController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerComienzos(10))
                .Returns(OperationResult<IEnumerable<DtoComienzoResponse>>.Ok(
                    [new DtoComienzoResponse { IdProceso = 20, NombreProceso = "Marzo" }],
                    nameof(ICatalogosService.ObtenerComienzos)));

            var response = controller.ObtenerComienzos(10);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ObtenerCarreras_ReturnsOk()
        {
            var serviceMock = new Mock<ICatalogosService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<CatalogosController>>();
            var controller = new CatalogosController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerCarreras())
                .Returns(OperationResult<IEnumerable<DtoCarreraResponse>>.Ok(
                    [new DtoCarreraResponse { IdProducto = 10, NombreProducto = "ATI" }],
                    nameof(ICatalogosService.ObtenerCarreras)));

            var response = controller.ObtenerCarreras();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Theory]
        [InlineData(nameof(CatalogosController.ObtenerPaisesEstadosCiudades))]
        [InlineData(nameof(CatalogosController.ObtenerTipoDocumentos))]
        [InlineData(nameof(CatalogosController.ObtenerCarreras))]
        [InlineData(nameof(CatalogosController.ObtenerComienzos))]
        public void PublicEndpoints_HaveAllowAnonymous(string methodName)
        {
            var method = typeof(CatalogosController).GetMethod(methodName);

            Assert.NotNull(method);
            Assert.Contains(
                method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true),
                attribute => attribute is AllowAnonymousAttribute);
        }
    }
}
