using AppLogic.DevartDTOs;
using AppLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Controllers
{
    public class PreinscripcionControllerTests
    {
        [Fact]
        public void ObtenerTurnos_ReturnsOk()
        {
            var serviceMock = new Mock<IPreinscripcionService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<PreinscripcionController>>();
            var controller = new PreinscripcionController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerTurnos(1, 2))
                .Returns(OperationResult<IEnumerable<DtoTurnoDevart>>.Ok(new List<DtoTurnoDevart>(), nameof(IPreinscripcionService.ObtenerTurnos)));

            var response = controller.ObtenerTurnos(1, 2);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
