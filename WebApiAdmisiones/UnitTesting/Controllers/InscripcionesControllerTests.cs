using AppLogic.DevartDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using Xunit;
using WebApiAdmisiones.Security.Authentication;
using AppLogic.IServices.Inscripciones;

namespace UnitTesting.Controllers
{
    public class InscripcionesControllerTests
    {

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
