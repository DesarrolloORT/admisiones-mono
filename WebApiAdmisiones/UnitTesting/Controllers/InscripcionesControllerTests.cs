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
        public void ObtenerUltimaInscripcionActiva_ReturnsOk()
        {
            var serviceMock = new Mock<IInscripcionesService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<InscripcionesController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            var controller = new InscripcionesController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerUltimaInscripcionActiva(1))
                .Returns(OperationResult<DTOUltimaInscripcion>.Ok(new DTOUltimaInscripcion(), nameof(IInscripcionesService.ObtenerUltimaInscripcionActiva)));

            var response = controller.ObtenerUltimaInscripcionActiva();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
