using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Controllers
{
    public class LoginControllerTests
    {
        [Fact]
        public async Task Login_ReturnsOk()
        {
            var serviceMock = new Mock<ILoginService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var loggerMock = new Mock<ILogger<LoginController>>();
            var controller = new LoginController(serviceMock.Object, loggerMock.Object, currentUserMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            serviceMock.Setup(s => s.AutenticarUsuarioLDAPAsync(1, "pwd"))
                .ReturnsAsync(OperationResult<DTOAuthenticationResponse>.Ok(
                    new DTOAuthenticationResponse
                    {
                        Persona = new DTOPersonaAuth
                        {
                            CodigoPersona = 1
                        },
                        AccessToken = "access-token",
                        RefreshToken = "refresh-token"
                    },
                    nameof(ILoginService.AutenticarUsuarioLDAPAsync)));

            var response = await controller.Login(new LoginRequest { CodigoPersona = 1, Password = "pwd" });

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
