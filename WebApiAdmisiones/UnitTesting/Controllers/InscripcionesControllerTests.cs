using AppLogic.Inscripciones.Dtos;
using AppLogic.Inscripciones.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security.Authentication;
using Xunit;

namespace UnitTesting.Controllers
{
    public class InscripcionesControllerTests
    {
        [Fact]
        public async Task ConfirmarPreInscripcion_DelegatesToServiceWithAuthenticatedUser()
        {
            var serviceMock = new Mock<IInscripcionesService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<InscripcionesController>>();
            var request = new DtoConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
            };
            var responseDto = new DtoConfirmarPreInscripcionResponse
            {
                Confirmada = true,
                IdInscripcion = 100,
                FechaVencimientoPago = new DateTime(2026, 6, 30),
                Senia = 1500
            };

            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            serviceMock
                .Setup(s => s.ConfirmarPreInscripcion(1, request))
                .ReturnsAsync(OperationResult<DtoConfirmarPreInscripcionResponse>.Ok(responseDto, nameof(IInscripcionesService.ConfirmarPreInscripcion)));

            var controller = new InscripcionesController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = await controller.ConfirmarPreInscripcion(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.ConfirmarPreInscripcion(1, request), Times.Once);
        }

        [Fact]
        public async Task ReactivarInscripcion_DelegatesToServiceWithAuthenticatedUser()
        {
            var serviceMock = new Mock<IInscripcionesService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<InscripcionesController>>();
            var request = new DtoReactivarInscripcionRequest { IdInscripto = 555 };
            var responseDto = new DtoConfirmarPreInscripcionResponse
            {
                Confirmada = true,
                IdInscripcion = 100,
                Senia = 1500
            };

            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            serviceMock
                .Setup(s => s.ReactivarInscripcion(1, request))
                .ReturnsAsync(OperationResult<DtoConfirmarPreInscripcionResponse>.Ok(responseDto, nameof(IInscripcionesService.ReactivarInscripcion)));

            var controller = new InscripcionesController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = await controller.ReactivarInscripcion(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.ReactivarInscripcion(1, request), Times.Once);
        }

        [Fact]
        public async Task Pagar_DelegatesToServiceWithAuthenticatedUser()
        {
            var serviceMock = new Mock<IInscripcionesService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<InscripcionesController>>();
            var request = new DtoPagarRequest { IdInscripto = 555, TipoPago = "BANRED" };

            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            serviceMock
                .Setup(s => s.Pagar(1, request))
                .ReturnsAsync(OperationResult<DtoPagarResponse>.Ok(
                    new DtoPagarResponse { Resultado = "URL_GENERADA", UrlPago = "https://pagos.test" },
                    nameof(IInscripcionesService.Pagar)));
            var controller = new InscripcionesController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = await controller.Pagar(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.Pagar(1, request), Times.Once);
        }

        [Fact]
        public void ReglamentoEstudiantil_PostEndpoint_IsNotExposed()
        {
            var postRoutes = typeof(InscripcionesController)
                .GetMethods()
                .SelectMany(method => method.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false).Cast<HttpPostAttribute>())
                .Select(attribute => attribute.Template);

            Assert.DoesNotContain("ReglamentoEstudiantil", postRoutes);
        }

        [Fact]
        public void ObtenerAceptacionReglamentoEstudiantil_DelegatesToServiceWithAuthenticatedUser()
        {
            var serviceMock = new Mock<IInscripcionesService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<InscripcionesController>>();
            var responseDto = new DtoAceptacionReglamentoEstudiantilResponse
            {
                AceptoReglamentoEstudiantil = true,
                FechaAceptacion = new DateTime(2026, 6, 1)
            };

            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            serviceMock
                .Setup(s => s.ObtenerAceptacionReglamentoEstudiantil(1))
                .Returns(OperationResult<DtoAceptacionReglamentoEstudiantilResponse>.Ok(
                    responseDto,
                    nameof(IInscripcionesService.ObtenerAceptacionReglamentoEstudiantil)));
            var controller = new InscripcionesController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.ObtenerAceptacionReglamentoEstudiantil();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.ObtenerAceptacionReglamentoEstudiantil(1), Times.Once);
        }

    }
}
