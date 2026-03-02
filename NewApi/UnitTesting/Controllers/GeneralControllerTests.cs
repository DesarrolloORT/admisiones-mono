using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using WebApiFDP.Controllers;
using AppLogic.Interfaces;
using AppLogic.DevartDTOs;
using Utilities;
using WebApiFDP.Security;

namespace UnitTesting.Controllers
{
    public class GeneralControllerTests
    {
        private readonly Mock<IGeneralServices> _fdpServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<ILogger<GeneralController>> _loggerMock;
        private readonly GeneralController _controller;

        public GeneralControllerTests()
        {
            _fdpServiceMock = new Mock<IGeneralServices>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<GeneralController>>();
            _controller = new GeneralController(_fdpServiceMock.Object, _loggerMock.Object, _currentUserMock.Object);
        }

        [Fact]
        public void ObtenerPais_ReturnsOk()
        {
            var pais = new DtoPaisDevart { CodigoPais = 1, Nombre = "Uruguay" };
            _fdpServiceMock.Setup(s => s.ObtenerPais(1))
                .Returns(OperationResult<DtoPaisDevart>.Ok(pais, nameof(IGeneralServices.ObtenerPais)));

            var response = _controller.ObtenerPais(1);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            var wrapper = Assert.IsType<OperationResult<DtoPaisDevart>>(okResult.Value);
            Assert.Equal(pais, wrapper.Data);
        }

        [Fact]
        public void ObtenerPaises_ReturnsOk()
        {
            var paises = new List<DtoPaisDevart>();
            _fdpServiceMock.Setup(s => s.ObtenerPaises())
                .Returns(OperationResult<IEnumerable<DtoPaisDevart>>.Ok(paises, nameof(IGeneralServices.ObtenerPaises)));

            var response = _controller.ObtenerPaises();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            var wrapper = Assert.IsType<OperationResult<IEnumerable<DtoPaisDevart>>>(okResult.Value);
            Assert.Equal(paises, wrapper.Data);
        }

        
    }
}
