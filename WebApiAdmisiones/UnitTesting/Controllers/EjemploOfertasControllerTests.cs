using AppLogic.ApiClients;
using AppLogic.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security;

namespace UnitTesting.Controllers
{
    public class EjemploOfertasControllerTests
    {
        [Fact]
        public async Task ObtenerMisOfertas_WithoutCurrentUser_ReturnsUnauthorized()
        {
            var serviceMock = new Mock<IOfertasInscripcionService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var controller = CrearController(serviceMock.Object, currentUserMock.Object);

            var response = await controller.ObtenerMisOfertas(10, 20, 30);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(response);
            Assert.Equal(401, unauthorized.StatusCode);
            serviceMock.Verify(
                s => s.ObtenerOfertasParaPersonaAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task ObtenerMisOfertas_WithCurrentUser_DelegatesToServiceAndReturnsOk()
        {
            var serviceMock = new Mock<IOfertasInscripcionService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            currentUserMock.Setup(c => c.UserId).Returns(123);
            var result = OperationResult<OfertasInscripcionResponse>.Ok(
                new OfertasInscripcionResponse
                {
                    TotalCount = 1,
                    Ofertas =
                    [
                        new OfertaInscripcionDto
                        {
                            IdOferta = 44,
                            IdProducto = 10,
                            IdTurno = 30,
                            Disponible = true
                        }
                    ]
                },
                nameof(IOfertasInscripcionService.ObtenerOfertasParaPersonaAsync));
            serviceMock
                .Setup(s => s.ObtenerOfertasParaPersonaAsync(123, 10, 20, 30))
                .ReturnsAsync(result);
            var controller = CrearController(serviceMock.Object, currentUserMock.Object);

            var response = await controller.ObtenerMisOfertas(10, 20, 30);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            serviceMock.Verify(s => s.ObtenerOfertasParaPersonaAsync(123, 10, 20, 30), Times.Once);
        }

        private static EjemploOfertasController CrearController(
            IOfertasInscripcionService service,
            ICurrentUserService currentUser)
        {
            return new EjemploOfertasController(
                service,
                Mock.Of<ILogger<EjemploOfertasController>>(),
                currentUser)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }
    }
}
