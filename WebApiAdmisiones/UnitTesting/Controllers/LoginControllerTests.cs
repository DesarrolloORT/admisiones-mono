using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Controllers
{
    public class LoginControllerTests
    {
        private static AuthController CrearController(
            Mock<IAuthService> serviceMock,
            ClaimsPrincipal? user = null,
            string? cookieHeader = null)
        {
            var controller = new AuthController(
                serviceMock.Object,
                new Mock<ILogger<AuthController>>().Object,
                new Mock<ICurrentUserService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            controller.ControllerContext.HttpContext.User = user ?? new ClaimsPrincipal(new ClaimsIdentity());
            if (cookieHeader is not null)
            {
                controller.ControllerContext.HttpContext.Request.Headers.Cookie = cookieHeader;
            }

            return controller;
        }

        [Fact]
        public async Task Login_ReturnsOk()
        {
            var serviceMock = new Mock<IAuthService>();
            var controller = CrearController(serviceMock);

            serviceMock.Setup(s => s.AutenticarUsuarioLDAPAsync(1, "pwd"))
                .ReturnsAsync(OperationResult<DtoAuthenticationResponse>.Ok(
                    new DtoAuthenticationResponse
                    {
                        Persona = new DtoPersonaAuth
                        {
                            CodigoPersona = 1
                        },
                        AccessToken = "access-token",
                        RefreshToken = "refresh-token"
                    },
                    nameof(IAuthService.AutenticarUsuarioLDAPAsync)));

            var response = await controller.Login(new LoginRequest { CodigoPersona = 1, Password = "pwd" });

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Login_WhenAuthenticationFails_ReturnsUnauthorized()
        {
            var serviceMock = new Mock<IAuthService>();
            var controller = CrearController(serviceMock);

            serviceMock.Setup(s => s.AutenticarUsuarioLDAPAsync(1, "pwd"))
                .ReturnsAsync(OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "ERR",
                    nameof(IAuthService.AutenticarUsuarioLDAPAsync),
                    "Credenciales invalidas",
                    401));

            var response = await controller.Login(new LoginRequest { CodigoPersona = 1, Password = "pwd" });

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(401, objectResult.StatusCode);
        }

        [Fact]
        public void Logout_ReturnsOk()
        {
            var serviceMock = new Mock<IAuthService>();
            var controller = CrearController(serviceMock);

            var response = controller.Logout();

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_WhenUnauthorized_ReturnsUnauthorized()
        {
            var serviceMock = new Mock<IAuthService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "123")
            ], "TestAuth"));
            var controller = CrearController(
                serviceMock,
                user,
                $"{CookieAuthenticationHelper.RefreshTokenCookieName}=refresh-token");

            serviceMock.Setup(s => s.RefrescarTokensAsync("refresh-token", "123"))
                .ReturnsAsync(OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "ERR",
                    nameof(IAuthService.RefrescarTokensAsync),
                    "Refresh invalido",
                    401));

            var response = await controller.RefreshToken();

            var objectResult = Assert.IsType<UnauthorizedObjectResult>(response);
            Assert.Equal(401, objectResult.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_WhenNotFound_ReturnsNotFound()
        {
            var serviceMock = new Mock<IAuthService>();
            var controller = CrearController(serviceMock);

            serviceMock.Setup(s => s.RefrescarTokensAsync(null, null))
                .ReturnsAsync(OperationResult<DtoAuthenticationResponse>.IsFailed(
                    "ERR",
                    nameof(IAuthService.RefrescarTokensAsync),
                    "Usuario no encontrado",
                    404));

            var response = await controller.RefreshToken();

            var objectResult = Assert.IsType<NotFoundObjectResult>(response);
            Assert.Equal(404, objectResult.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_WhenSuccessfulWithoutData_ReturnsOk()
        {
            var serviceMock = new Mock<IAuthService>();
            var controller = CrearController(serviceMock);

            serviceMock.Setup(s => s.RefrescarTokensAsync(null, null))
                .ReturnsAsync(OperationResult<DtoAuthenticationResponse>.Ok(
                    null,
                    nameof(IAuthService.RefrescarTokensAsync)));

            var response = await controller.RefreshToken();

            var objectResult = Assert.IsType<OkObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_WhenSuccessfulWithData_ReturnsOk()
        {
            var serviceMock = new Mock<IAuthService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "123")
            ], "TestAuth"));
            var controller = CrearController(
                serviceMock,
                user,
                $"{CookieAuthenticationHelper.RefreshTokenCookieName}=refresh-token");

            serviceMock.Setup(s => s.RefrescarTokensAsync("refresh-token", "123"))
                .ReturnsAsync(OperationResult<DtoAuthenticationResponse>.Ok(
                    new DtoAuthenticationResponse
                    {
                        Persona = new DtoPersonaAuth { CodigoPersona = 123 },
                        AccessToken = "new-access",
                        RefreshToken = "new-refresh"
                    },
                    nameof(IAuthService.RefrescarTokensAsync)));

            var response = await controller.RefreshToken();

            var objectResult = Assert.IsType<OkObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
        }
    }
}
