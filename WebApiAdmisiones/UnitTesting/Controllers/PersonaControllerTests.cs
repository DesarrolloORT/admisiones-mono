using AppLogic.DTOs;
using AppLogic.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using Xunit;
using WebApiAdmisiones.Security.Authentication;
using AppLogic.IServices.Personas;

namespace UnitTesting.Controllers
{
    public class PersonaControllerTests
    {
        private readonly Mock<IPersonaService> _personaServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly PersonaController _controller;

        public PersonaControllerTests()
        {
            _personaServiceMock = new Mock<IPersonaService>();
            _currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<PersonaController>>();

            _controller = new PersonaController(
                _personaServiceMock.Object,
                loggerMock.Object,
                _currentUserMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public void ObtenerDatosPersona_UsesAuthenticatedUserAndReturnsOk()
        {
            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _personaServiceMock
                .Setup(s => s.ObtenerDatosPersona(123))
                .Returns(OperationResult<DtoDatosPersona>.Ok(new DtoDatosPersona(), nameof(IPersonaService.ObtenerDatosPersona)));

            var response = _controller.ObtenerDatosPersona();

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _personaServiceMock.Verify(s => s.ObtenerDatosPersona(123), Times.Once);
        }

        [Fact]
        public void ActualizarDatosPersona_UsesAuthenticatedUserAndReturnsOk()
        {
            var request = new ActualizarDatosPersonaRequest
            {
                CodigoPais = 1,
                CodigoEstado = 2,
                CodigoCiudad = 3,
                Direccion = "18 de julio 1234",
                Telefono1 = "24001234",
                Mail = "ana@test.com",
                VerificacionMail = "ana@test.com"
            };

            _currentUserMock.Setup(c => c.GetUserId()).Returns(123);
            _personaServiceMock
                .Setup(s => s.ActualizarDatosPersona(123, request))
                .Returns(OperationResult<bool>.Ok(true, nameof(IPersonaService.ActualizarDatosPersona)));

            var response = _controller.ActualizarDatosPersona(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _personaServiceMock.Verify(s => s.ActualizarDatosPersona(123, request), Times.Once);
        }

        [Fact]
        public async Task CambiarPassword_UsesAuthenticatedUserAndReturnsOk()
        {
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };
            var result = OperationResult<object>.Ok(
                "Se actualizó tu contraseña",
                nameof(IPersonaService.CambiarPasswordAsync));

            _currentUserMock.Setup(c => c.UserId).Returns(123);
            _personaServiceMock
                .Setup(s => s.CambiarPasswordAsync(123, request))
                .ReturnsAsync(result);

            var response = await _controller.CambiarPassword(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _personaServiceMock.Verify(s => s.CambiarPasswordAsync(123, request), Times.Once);
        }

        [Fact]
        public async Task CambiarPassword_WithoutAuthenticatedUser_ReturnsUnauthorized()
        {
            var request = new DtoCambiarPasswordRequest
            {
                PasswordActual = "Password123!",
                PasswordNueva = "NuevaPassword1!"
            };

            var response = await _controller.CambiarPassword(request);

            var unauthorizedResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<object>>(unauthorizedResult.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("CAM_PAS_03", operationResult.ErrorCode);
            _personaServiceMock.Verify(
                s => s.CambiarPasswordAsync(It.IsAny<long>(), It.IsAny<DtoCambiarPasswordRequest>()),
                Times.Never);
        }
    }
}
