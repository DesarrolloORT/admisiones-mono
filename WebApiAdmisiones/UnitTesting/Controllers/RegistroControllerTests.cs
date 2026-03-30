using System.Collections.Generic;
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
    public class RegistroControllerTests
    {
        [Fact]
        public void ObtenerPaises_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerPaises())
                .Returns(OperationResult<IEnumerable<DtoPaisDevart>>.Ok(
                    [new DtoPaisDevart { CodigoPais = 1, Nombre = "Uruguay" }],
                    nameof(IRegistroService.ObtenerPaises)));

            var response = controller.ObtenerPaises();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ObtenerPaisesEstadosCiudades_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerPaisesEstadosCiudades())
                .Returns(OperationResult<IEnumerable<DtoPaisDevart>>.Ok(
                    [new DtoPaisDevart { CodigoPais = 1, Nombre = "Uruguay" }],
                    nameof(IRegistroService.ObtenerPaisesEstadosCiudades)));

            var response = controller.ObtenerPaisesEstadosCiudades();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ObtenerTipoDocumentos_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerTipoDocumentos())
                .Returns(OperationResult<IEnumerable<DtoAcaTipoDocumentoDevart>>.Ok(
                    [new DtoAcaTipoDocumentoDevart { CodTipoDocumento = 1, Descripcion = "Cedula" }],
                    nameof(IRegistroService.ObtenerTipoDocumentos)));

            var response = controller.ObtenerTipoDocumentos();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ObtenerProcesosHabilitadosPorProducto_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerProcesosHabilitadosPorProducto(10))
                .Returns(OperationResult<IEnumerable<DtoProcesoDevart>>.Ok(
                    [new DtoProcesoDevart { IdProceso = 20, NombreProceso = "Marzo" }],
                    nameof(IRegistroService.ObtenerProcesosHabilitadosPorProducto)));

            var response = controller.ObtenerProcesosHabilitadosPorProducto(10);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ObtenerProductosVigentes_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerProductosVigentes())
                .Returns(OperationResult<IEnumerable<DtoProductoAdmisiones>>.Ok(
                    [new DtoProductoAdmisiones { IdProducto = 10, NombreProducto = "ATI" }],
                    nameof(IRegistroService.ObtenerProductosVigentes)));

            var response = controller.ObtenerProductosVigentes();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Theory]
        [InlineData(nameof(RegistroController.ObtenerPaises))]
        [InlineData(nameof(RegistroController.ObtenerTipoDocumentos))]
        [InlineData(nameof(RegistroController.ObtenerProcesosHabilitadosPorProducto))]
        [InlineData(nameof(RegistroController.ObtenerPaisesEstadosCiudades))]
        [InlineData(nameof(RegistroController.ObtenerProductosVigentes))]
        public void PublicEndpoints_HaveAllowAnonymous(string methodName)
        {
            var method = typeof(RegistroController).GetMethod(methodName);

            Assert.NotNull(method);
            Assert.Contains(
                method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true),
                attribute => attribute is AllowAnonymousAttribute);
        }
    }
}
