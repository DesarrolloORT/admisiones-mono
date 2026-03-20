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
    public class PersonaControllerTests
    {
        [Fact]
        public void ObtenerPersona_ReturnsOk()
        {
            var serviceMock = new Mock<IPersonaAdmisionService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<PersonaController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            var controller = new PersonaController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerPersona(1))
                .Returns(OperationResult<DtoPersonaDevart>.Ok(new DtoPersonaDevart(), nameof(IPersonaAdmisionService.ObtenerPersona)));

            var response = controller.ObtenerPersona();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
