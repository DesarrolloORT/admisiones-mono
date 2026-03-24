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
    public class CatalogosControllerTests
    {
        [Fact]
        public void ObtenerPais_ReturnsOk()
        {
            var serviceMock = new Mock<ICatalogosService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<CatalogosController>>();
            var controller = new CatalogosController(serviceMock.Object, loggerMock.Object, currentUserMock.Object);
            var pais = new DtoPaisDevart { CodigoPais = 1, Nombre = "Uruguay" };

            serviceMock.Setup(s => s.ObtenerPais(1))
                .Returns(OperationResult<DtoPaisDevart>.Ok(pais, nameof(ICatalogosService.ObtenerPais)));

            var response = controller.ObtenerPais(1);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
