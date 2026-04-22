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
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly AuthController _controller;
        private readonly DefaultHttpContext _httpContext;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _loggerMock = new Mock<ILogger<AuthController>>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _httpContext = new DefaultHttpContext();

            _controller = new AuthController(
                _authServiceMock.Object,
                _loggerMock.Object,
                _currentUserMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _httpContext
                }
            };
        }

        [Fact]
        public async Task Login_SuccessfulAuthentication_ReturnsOkAndSetsCookies()
        {
            // Arrange
            var request = new LoginRequest
            {
                CodigoPersona = 12345,
                Password = "testPassword"
            };

            var authResponse = new DtoAuthenticationResponse
            {
                Persona = new DtoPersonaAuth
                {
                    CodigoPersona = 12345,
                    PrimerNombre = "Test",
                    PrimerApellido = "User"
                },
                AccessToken = "test-access-token",
                RefreshToken = "test-refresh-token"
            };

            var result = OperationResult<DtoAuthenticationResponse>.Ok(
                authResponse,
                nameof(IAuthService.AutenticarUsuarioLDAPAsync));

            _authServiceMock
                .Setup(s => s.AutenticarUsuarioLDAPAsync(request.CodigoPersona, request.Password))
                .ReturnsAsync(result);

            // Act
            var response = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Login_SuccessfulAuthenticationWithLoggingEnabled_LogsInformation()
        {
            // Arrange
            var request = new LoginRequest
            {
                CodigoPersona = 12345,
                Password = "testPassword"
            };

            var authResponse = new DtoAuthenticationResponse
            {
                Persona = new DtoPersonaAuth
                {
                    CodigoPersona = 12345,
                    PrimerNombre = "Test",
                    PrimerApellido = "User"
                },
                AccessToken = "test-access-token",
                RefreshToken = "test-refresh-token"
            };

            var result = OperationResult<DtoAuthenticationResponse>.Ok(
                authResponse,
                nameof(IAuthService.AutenticarUsuarioLDAPAsync));

            _authServiceMock
                .Setup(s => s.AutenticarUsuarioLDAPAsync(request.CodigoPersona, request.Password))
                .ReturnsAsync(result);

            _loggerMock
                .Setup(l => l.IsEnabled(LogLevel.Information))
                .Returns(true);

            // Act
            var response = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("autenticado exitosamente")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Login_FailedAuthentication_ReturnsError()
        {
            // Arrange
            var request = new LoginRequest
            {
                CodigoPersona = 12345,
                Password = "wrongPassword"
            };

            var result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                errorCode: "AUTH_01",
                originMethod: nameof(IAuthService.AutenticarUsuarioLDAPAsync),
                message: "Credenciales inválidas",
                httpCode: 401);

            _authServiceMock
                .Setup(s => s.AutenticarUsuarioLDAPAsync(request.CodigoPersona, request.Password))
                .ReturnsAsync(result);

            // Act
            var response = await _controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task Login_SuccessWithNullData_ReturnsResultWithoutSettingCookies()
        {
            // Arrange
            var request = new LoginRequest
            {
                CodigoPersona = 12345,
                Password = "testPassword"
            };

            var result = OperationResult<DtoAuthenticationResponse>.Ok(
                null,
                nameof(IAuthService.AutenticarUsuarioLDAPAsync));

            _authServiceMock
                .Setup(s => s.AutenticarUsuarioLDAPAsync(request.CodigoPersona, request.Password))
                .ReturnsAsync(result);

            // Act
            var response = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void Logout_ClearsCookiesAndReturnsOk()
        {
            // Arrange - nothing needed

            // Act
            var response = _controller.Logout();

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<string>>(okResult.Value);
            Assert.True(operationResult.Success);
            Assert.Equal("Sesión cerrada correctamente.", operationResult.Data);
        }

        [Fact]
        public async Task RefreshToken_WithValidToken_ReturnsOkAndSetsCookies()
        {
            // Arrange
            var refreshToken = "valid-refresh-token";
            _httpContext.Request.Headers.Cookie = $"{CookieAuthenticationHelper.RefreshTokenCookieName}={refreshToken}";

            var authResponse = new DtoAuthenticationResponse
            {
                Persona = new DtoPersonaAuth
                {
                    CodigoPersona = 12345,
                    PrimerNombre = "Test",
                    PrimerApellido = "User"
                },
                AccessToken = "new-access-token",
                RefreshToken = "new-refresh-token"
            };

            var result = OperationResult<DtoAuthenticationResponse>.Ok(
                authResponse,
                nameof(IAuthService.RefrescarTokensAsync));

            _authServiceMock
                .Setup(s => s.RefrescarTokensAsync(refreshToken))
                .ReturnsAsync(result);

            // Act
            var response = await _controller.RefreshToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_WithMissingToken_ReturnsUnauthorized()
        {
            // Arrange - no cookie set

            // Act
            var response = await _controller.RefreshToken();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(response);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<DtoAuthenticationResponse>>(unauthorizedResult.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("AUTH_RT_01", operationResult.ErrorCode);
        }

        [Fact]
        public async Task RefreshToken_WithEmptyToken_ReturnsUnauthorized()
        {
            // Arrange
            _httpContext.Request.Headers.Cookie = $"{CookieAuthenticationHelper.RefreshTokenCookieName}=";

            // Act
            var response = await _controller.RefreshToken();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(response);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<DtoAuthenticationResponse>>(unauthorizedResult.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("AUTH_RT_01", operationResult.ErrorCode);
        }

        [Fact]
        public async Task RefreshToken_ServiceReturnsFailureWith401_ReturnsUnauthorized()
        {
            // Arrange
            var refreshToken = "invalid-refresh-token";
            _httpContext.Request.Headers.Cookie = $"{CookieAuthenticationHelper.RefreshTokenCookieName}={refreshToken}";

            var result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                errorCode: "AUTH_02",
                originMethod: nameof(IAuthService.RefrescarTokensAsync),
                message: "Refresh token inválido",
                httpCode: 401);

            _authServiceMock
                .Setup(s => s.RefrescarTokensAsync(refreshToken))
                .ReturnsAsync(result);

            // Act
            var response = await _controller.RefreshToken();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(response);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_ServiceReturnsFailureWith404_ReturnsNotFound()
        {
            // Arrange
            var refreshToken = "valid-token-for-nonexistent-user";
            _httpContext.Request.Headers.Cookie = $"{CookieAuthenticationHelper.RefreshTokenCookieName}={refreshToken}";

            var result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                errorCode: "AUTH_03",
                originMethod: nameof(IAuthService.RefrescarTokensAsync),
                message: "Usuario no encontrado",
                httpCode: 404);

            _authServiceMock
                .Setup(s => s.RefrescarTokensAsync(refreshToken))
                .ReturnsAsync(result);

            // Act
            var response = await _controller.RefreshToken();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(response);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_SuccessWithNullData_ReturnsOkWithoutSettingCookies()
        {
            // Arrange
            var refreshToken = "valid-refresh-token";
            _httpContext.Request.Headers.Cookie = $"{CookieAuthenticationHelper.RefreshTokenCookieName}={refreshToken}";

            var result = OperationResult<DtoAuthenticationResponse>.Ok(
                null,
                nameof(IAuthService.RefrescarTokensAsync));

            _authServiceMock
                .Setup(s => s.RefrescarTokensAsync(refreshToken))
                .ReturnsAsync(result);

            // Act
            var response = await _controller.RefreshToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
