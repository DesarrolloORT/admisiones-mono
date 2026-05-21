using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Controllers
{
    public class InscripcionesControllerTests
    {
        [Fact]
        public void ObtenerMisInscripciones_ReturnsOkAndUsesCurrentUser()
        {
            var serviceMock = new Mock<IInscripcionesService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<InscripcionesController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            currentUserMock.Setup(c => c.UserId).Returns(1);
            var controller = new InscripcionesController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerMisInscripciones(1))
                .Returns(OperationResult<IEnumerable<DtoInscripcionHome>>.Ok(
                [
                    new DtoInscripcionHome
                    {
                        Estado = "Confirmada",
                        IdProducto = 10,
                        NombreProducto = "Analista en Tecnologias de la Informacion",
                        IdComienzo = 20,
                        NombreComienzo = "Marzo",
                        IdTurno = 30,
                        NombreTurno = "Nocturno"
                    }
                ], nameof(IInscripcionesService.ObtenerMisInscripciones)));

            var response = controller.ObtenerMisInscripciones();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.ObtenerMisInscripciones(1), Times.Once);
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
            var request = new InteresProductoRequest { IdProducto = 10, IdProcesoSeleccionado = 20 };

            serviceMock.Setup(s => s.RegistrarInteresProducto(1, request))
                .Returns(OperationResult<bool>.Ok(true, nameof(IInscripcionesService.RegistrarInteresProducto)));

            var response = controller.RegistrarInteresProducto(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
        */
    }
}
