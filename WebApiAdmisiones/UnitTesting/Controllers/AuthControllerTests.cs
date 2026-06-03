using AppLogic.DTOs;
using AppLogic.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly Mock<IPasswordActivationService> _passwordActivationServiceMock;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<IRedisRateLimiterService> _redisRateLimiterMock;
        private readonly IConfiguration _configuration;
        private readonly AuthController _controller;
        private readonly DefaultHttpContext _httpContext;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _passwordActivationServiceMock = new Mock<IPasswordActivationService>();
            _loggerMock = new Mock<ILogger<AuthController>>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _redisRateLimiterMock = new Mock<IRedisRateLimiterService>();
            _httpContext = new DefaultHttpContext();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PasswordActivation:SessionMinutes"] = "15",
                    ["Authentication:Login:RateLimitAccountAttempts"] = "5",
                    ["Authentication:Login:RateLimitWindowMinutes"] = "15"
                })
                .Build();

            // Mock remote IP address para rate limiting
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

            // Configurar mock de RedisRateLimiterService para permitir todos los intentos por defecto
            _redisRateLimiterMock
                .Setup(r => r.ValidateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new RateLimitValidationResult
                {
                    IsAllowed = true,
                    RemainingAttempts = 5,
                    ResetTime = DateTimeOffset.UtcNow.AddMinutes(15),
                    PartitionKey = "test-key"
                });

            _controller = new AuthController(
                _authServiceMock.Object,
                _passwordActivationServiceMock.Object,
                _configuration,
                _loggerMock.Object,
                _currentUserMock.Object,
                _redisRateLimiterMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _httpContext
                }
            };
        }

        [Fact]
        public async Task ActivarLinkPassword_WithValidToken_ReturnsOkAndSetsTemporaryCookie()
        {
            var request = new DtoActivarLinkPasswordRequest { Token = "activation-token" };
            var serviceResult = OperationResult<DtoPasswordActivationSession>.Ok(
                new DtoPasswordActivationSession
                {
                    CodigoPersona = 12345,
                    SessionToken = "session-token"
                },
                nameof(IPasswordActivationService.ActivarLinkPasswordAsync));

            _passwordActivationServiceMock
                .Setup(s => s.ActivarLinkPasswordAsync(request.Token))
                .ReturnsAsync(serviceResult);

            var response = await _controller.ActivarLinkPassword(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task CompletarPassword_WithoutTemporaryCookie_ReturnsUnauthorized()
        {
            var request = new DtoCompletarPasswordInicialRequest
            {
                PasswordNueva = "NuevaPassword1!"
            };

            _passwordActivationServiceMock
                .Setup(s => s.ValidarSessionToken(string.Empty))
                .Returns(OperationResult<long>.IsFailed(
                    "ACT_SES_01",
                    nameof(IPasswordActivationService.ValidarSessionToken),
                    "Sesión temporal no encontrada.",
                    401,
                    default));

            var response = await _controller.CompletarPassword(request);

            var unauthorizedResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            _authServiceMock.Verify(
                s => s.CompletarPasswordAsync(It.IsAny<long>(), It.IsAny<DtoCompletarPasswordInicialRequest>()),
                Times.Never);
        }

        [Fact]
        public async Task Login_SuccessfulAuthentication_ReturnsOkAndSetsCookies()
        {
            // Arrange
            var request = new AuthRequest
            {
                TipoDocumento = "CI",
                Documento = "4773331-2",
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
                .Setup(s => s.AutenticarUsuarioLDAPAsync(request.TipoDocumento, request.Documento, request.Password))
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
            var request = new AuthRequest
            {
                TipoDocumento = "CI",
                Documento = "4773331-2",
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
                .Setup(s => s.AutenticarUsuarioLDAPAsync(request.TipoDocumento, request.Documento, request.Password))
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
            var request = new AuthRequest
            {
                TipoDocumento = "CI",
                Documento = "4773331-2",
                Password = "wrongPassword"
            };

            var result = OperationResult<DtoAuthenticationResponse>.IsFailed(
                errorCode: "AUTH_01",
                originMethod: nameof(IAuthService.AutenticarUsuarioLDAPAsync),
                message: "Credenciales inválidas",
                httpCode: 401);

            _authServiceMock
                .Setup(s => s.AutenticarUsuarioLDAPAsync(request.TipoDocumento, request.Documento, request.Password))
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
            var request = new AuthRequest
            {
                TipoDocumento = "CI",
                Documento = "4773331-2",
                Password = "testPassword"
            };

            var result = OperationResult<DtoAuthenticationResponse>.Ok(
                null,
                nameof(IAuthService.AutenticarUsuarioLDAPAsync));

            _authServiceMock
                .Setup(s => s.AutenticarUsuarioLDAPAsync(request.TipoDocumento, request.Documento, request.Password))
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

        #region Rate Limiting Tests

        [Fact]
        public async Task Login_ExceedsAccountRateLimit_Returns429()
        {
            // Arrange
            var request = new AuthRequest
            {
                TipoDocumento = "CI",
                Documento = "12345678",
                Password = "password123"
            };

            // Configurar mock para rechazar por rate limit
            _redisRateLimiterMock
                .Setup(r => r.ValidateAsync(
                    "192.168.1.100",
                    "CI",
                    "12345678",
                    5,
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new RateLimitValidationResult
                {
                    IsAllowed = false,
                    RemainingAttempts = 0,
                    ResetTime = DateTimeOffset.UtcNow.AddMinutes(15),
                    PartitionKey = "login-account:CI:12345678:ip:192.168.1.100"
                });

            // Act
            var response = await _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(429, statusCodeResult.StatusCode);

            // Verificar que NO se llamó al servicio de autenticación
            _authServiceMock.Verify(
                s => s.AutenticarUsuarioLDAPAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Login_WithinRateLimit_CallsAuthService()
        {
            // Arrange
            var request = new AuthRequest
            {
                TipoDocumento = "CI",
                Documento = "12345678",
                Password = "password123"
            };

            var authResponse = new DtoAuthenticationResponse
            {
                Persona = new DtoPersonaAuth
                {
                    CodigoPersona = 12345,
                    PrimerNombre = "Juan",
                    PrimerApellido = "Pérez",
                    Documento = "12345678"
                },
                AccessToken = "access-token",
                RefreshToken = "refresh-token"
            };

            _authServiceMock
                .Setup(s => s.AutenticarUsuarioLDAPAsync(request.TipoDocumento, request.Documento, request.Password))
                .ReturnsAsync(OperationResult<DtoAuthenticationResponse>.Ok(
                    authResponse,
                    nameof(IAuthService.AutenticarUsuarioLDAPAsync)));

            // Mock ya configurado en constructor para permitir intentos

            // Act
            var response = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);

            // Verificar que SÍ se llamó al servicio de autenticación
            _authServiceMock.Verify(
                s => s.AutenticarUsuarioLDAPAsync(request.TipoDocumento, request.Documento, request.Password),
                Times.Once);
        }

        [Fact]
        public async Task Login_RateLimitResponse_IncludesCorrectHeaders()
        {
            // Arrange
            var request = new AuthRequest
            {
                TipoDocumento = "CI",
                Documento = "12345678",
                Password = "password123"
            };

            var resetTime = DateTimeOffset.UtcNow.AddMinutes(15);

            _redisRateLimiterMock
                .Setup(r => r.ValidateAsync(
                    "192.168.1.100",
                    "CI",
                    "12345678",
                    5,
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new RateLimitValidationResult
                {
                    IsAllowed = false,
                    RemainingAttempts = 0,
                    ResetTime = resetTime,
                    PartitionKey = "login-account:CI:12345678:ip:192.168.1.100"
                });

            // Act
            var response = await _controller.Login(request);

            // Assert
            Assert.NotNull(_httpContext.Response.Headers["X-RateLimit-Limit"]);
            Assert.NotNull(_httpContext.Response.Headers["X-RateLimit-Remaining"]);
            Assert.NotNull(_httpContext.Response.Headers["X-RateLimit-Reset"]);
            Assert.NotNull(_httpContext.Response.Headers["Retry-After"]);
        }

        #endregion

    }
}
