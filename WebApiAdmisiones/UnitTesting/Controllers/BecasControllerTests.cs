using AppLogic.DevartDTOs;
using AppLogic.IServices.Becas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using Xunit;
using WebApiAdmisiones.Security.Authentication;

namespace UnitTesting.Controllers
{
    public class BecasControllerTests
    {
        [Fact]
        public void ObtenerMisInscripcionesConfirmadas_UsesAuthenticatedUserAndReturnsOk()
        {
            var serviceMock = new Mock<IBecasService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<BecasController>>();
            var controller = new BecasController(serviceMock.Object, loggerMock.Object, currentUserMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            serviceMock
                .Setup(s => s.ObtenerMisInscripcionesConfirmadas(123))
                .Returns(OperationResult<IEnumerable<DtoVdInscripcionesFresco1y2Devart>>.Ok(
                    [new DtoVdInscripcionesFresco1y2Devart { EstadoInscripcion = "Confirmada" }],
                    nameof(IBecasService.ObtenerMisInscripcionesConfirmadas)));

            var response = controller.ObtenerMisInscripcionesConfirmadas();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.ObtenerMisInscripcionesConfirmadas(123), Times.Once);
        }

        /*
        [Fact]
        public void ObtenerAceptacionReglamentoEstudiantil_ReturnsOk()
        {
            var serviceMock = new Mock<IBecasService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<BecasController>>();
            currentUserMock.Setup(c => c.GetUserId()).Returns(1);
            var controller = new BecasController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);

            serviceMock.Setup(s => s.ObtenerAceptacionReglamentoEstudiantil(1))
                .Returns(OperationResult<DtoAceptacionReglamentoEstDevart>.Ok(new DtoAceptacionReglamentoEstDevart(), nameof(IBecasService.ObtenerAceptacionReglamentoEstudiantil)));

            var response = controller.ObtenerAceptacionReglamentoEstudiantil();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
        */
    }
}
