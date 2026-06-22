using AppLogic.DevartDTOs;
using AppLogic.DTOs;
using AppLogic.IServices.Inscripciones;
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
            var request = new ConfirmarPreInscripcionRequest
            {
                AceptoReglamento = true,
                IdOfertaSeleccionada = 10
            };
            var responseDto = new ConfirmarPreInscripcionResponse
            {
                Confirmada = true,
                IdInscripcion = 100,
                SeniaInscripcion = 1500,
                FechaVencimientoPago = new DateTime(2026, 6, 30)
            };

            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            serviceMock
                .Setup(s => s.ConfirmarPreInscripcion(1, request))
                .ReturnsAsync(OperationResult<ConfirmarPreInscripcionResponse>.Ok(responseDto, nameof(IInscripcionesService.ConfirmarPreInscripcion)));

            var controller = new InscripcionesController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = await controller.ConfirmarPreInscripcion(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.ConfirmarPreInscripcion(1, request), Times.Once);
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
            var responseDto = new AceptacionReglamentoEstudiantilResponse
            {
                AceptoReglamentoEstudiantil = true,
                FechaAceptacion = new DateTime(2026, 6, 1)
            };

            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            serviceMock
                .Setup(s => s.ObtenerAceptacionReglamentoEstudiantil(1))
                .Returns(OperationResult<AceptacionReglamentoEstudiantilResponse>.Ok(
                    responseDto,
                    nameof(IInscripcionesService.ObtenerAceptacionReglamentoEstudiantil)));
            var controller = new InscripcionesController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            var response = controller.ObtenerAceptacionReglamentoEstudiantil();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.ObtenerAceptacionReglamentoEstudiantil(1), Times.Once);
        }

        /*
        [Fact]
        public void ObtenerUltimaInscripcionActiva_ReturnsOk()
        {
            var serviceMock = new Mock<IInscripcionesService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<InscripcionesController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            var controller = new InscripcionesController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerUltimaInscripcionActiva(1))
                .Returns(OperationResult<DtoUltimaInscripcion>.Ok(new DtoUltimaInscripcion(), nameof(IInscripcionesService.ObtenerUltimaInscripcionActiva)));

            var response = controller.ObtenerUltimaInscripcionActiva();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void RegistrarInteresProducto_ReturnsOk()
        {
            var serviceMock = new Mock<IInscripcionesService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<InscripcionesController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            var controller = new InscripcionesController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            var request = new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20, IdOferta = 30 };

            serviceMock.Setup(s => s.RegistrarInteresProducto(1, request))
                .Returns(OperationResult<bool>.Ok(true, nameof(IInscripcionesService.RegistrarInteresProducto)));

            var response = controller.RegistrarInteresProducto(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
        */
    }
}
