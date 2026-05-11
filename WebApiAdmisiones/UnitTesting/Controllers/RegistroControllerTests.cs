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
        public async Task EvaluarDocumento_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            var request = new RegistroEvaluarDocumentoRequest { TipoDocumento = "CI", Documento = "1234567-2" };

            serviceMock.Setup(s => s.EvaluarDocumentoAsync(request))
                .ReturnsAsync(OperationResult<RegistroEvaluacionResponse>.Ok(
                    new RegistroEvaluacionResponse { RequiereAltaPersona = true },
                    nameof(IRegistroService.EvaluarDocumentoAsync)));

            var response = await controller.EvaluarDocumento(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Confirmar_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            var request = new RegistroConfirmarRequest { TipoDocumento = "PS", Documento = "A123" };

            serviceMock.Setup(s => s.ConfirmarRegistroAsync(request))
                .ReturnsAsync(OperationResult<object?>.IsSuccess(
                    null,
                    nameof(IRegistroService.ConfirmarRegistroAsync),
                    "Registro realizado correctamente."));

            var response = await controller.Confirmar(request);

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
        public void ObtenerComienzos_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerComienzos(10))
                .Returns(OperationResult<IEnumerable<RegistroComienzoResponse>>.Ok(
                    [new RegistroComienzoResponse { IdProceso = 20, NombreProceso = "Marzo" }],
                    nameof(IRegistroService.ObtenerComienzos)));

            var response = controller.ObtenerComienzos(10);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void ObtenerCarreras_ReturnsOk()
        {
            var serviceMock = new Mock<IRegistroService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<RegistroController>>();
            var controller = new RegistroController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerCarreras())
                .Returns(OperationResult<IEnumerable<RegistroCarreraResponse>>.Ok(
                    [new RegistroCarreraResponse { IdProducto = 10, NombreProducto = "ATI" }],
                    nameof(IRegistroService.ObtenerCarreras)));

            var response = controller.ObtenerCarreras();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Theory]
        [InlineData(nameof(RegistroController.EvaluarDocumento))]
        [InlineData(nameof(RegistroController.Confirmar))]
        [InlineData(nameof(RegistroController.ObtenerTipoDocumentos))]
        [InlineData(nameof(RegistroController.ObtenerComienzos))]
        [InlineData(nameof(RegistroController.ObtenerPaisesEstadosCiudades))]
        [InlineData(nameof(RegistroController.ObtenerCarreras))]
        public void PublicEndpoints_HaveAllowAnonymous(string methodName)
        {
            var method = typeof(RegistroController).GetMethod(methodName);

            Assert.NotNull(method);
            Assert.Contains(
                method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true),
                attribute => attribute is AllowAnonymousAttribute);
        }

        [Fact]
        public void Confirmar_HasRequireCaptcha()
        {
            var method = typeof(RegistroController).GetMethod(nameof(RegistroController.Confirmar));

            Assert.NotNull(method);
            Assert.Contains(
                method!.GetCustomAttributes(typeof(RequireCaptchaAttribute), inherit: true),
                attribute => attribute is RequireCaptchaAttribute);
        }
    }
}
