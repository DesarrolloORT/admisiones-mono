using AppLogic.DTOs;
using AppLogic.IServices.Autenticacion;
using AppLogic.IServices.Registro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Controllers;
using WebApiAdmisiones.Security.Authentication;
using WebApiAdmisiones.Security.Captcha;

namespace UnitTesting.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<IPasswordActivationService> _passwordActivationServiceMock;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<IDosFactoresAuthService> _dosFactoresServiceMock;
        private readonly Mock<ILoginFlowService> _loginFlowServiceMock;
        private readonly Mock<IRegistroFlowService> _registroFlowServiceMock;
        private readonly IConfiguration _configuration;
        private readonly AuthController _controller;
        private readonly DefaultHttpContext _httpContext;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _passwordActivationServiceMock = new Mock<IPasswordActivationService>();
            _loggerMock = new Mock<ILogger<AuthController>>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _dosFactoresServiceMock = new Mock<IDosFactoresAuthService>();
            _loginFlowServiceMock = new Mock<ILoginFlowService>();
            _registroFlowServiceMock = new Mock<IRegistroFlowService>();
            _httpContext = new DefaultHttpContext();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PasswordActivation:SessionMinutes"] = "15"
                })
                .Build();

            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");
            _httpContext.SetRecaptchaScore(0.9);

            // Setup por defecto: login exitoso sin cookies (los tests de Login lo sobreescriben)
            _loginFlowServiceMock
                .Setup(s => s.EjecutarAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.LoginExitoso(
                    OperationResult<DtoAuthenticationResponse>.Ok(
                        new DtoAuthenticationResponse
                        {
                            Persona = new DtoPersonaAuth { CodigoPersona = 1 },
                            AccessToken = "token",
                            RefreshToken = "refresh",
                            RefreshTokenHash = "hash"
                        },
                        "EjecutarAsync")));

            _controller = new AuthController(
                _authServiceMock.Object,
                _passwordActivationServiceMock.Object,
                _configuration,
                _loggerMock.Object,
                _currentUserMock.Object,
                _dosFactoresServiceMock.Object,
                _loginFlowServiceMock.Object,
                _registroFlowServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _httpContext
                }
            };
        }

        [Theory]
        [InlineData(nameof(AuthController.Login), CaptchaActions.Login, CaptchaValidationMode.ScoreOnly)]
        [InlineData(nameof(AuthController.RecuperarPassword), CaptchaActions.RecuperarPassword, CaptchaValidationMode.RequireMinimumScore)]
        public void CaptchaEndpoints_HaveExpectedCaptchaAction(
            string methodName,
            string expectedAction,
            CaptchaValidationMode expectedMode)
        {
            var method = typeof(AuthController).GetMethod(methodName);

            Assert.NotNull(method);
            var attribute = Assert.Single(
                method!.GetCustomAttributes(typeof(RequireCaptchaAttribute), inherit: true)
                    .OfType<RequireCaptchaAttribute>());
            Assert.Equal(expectedMode, attribute.Arguments[0]);
            Assert.Equal(expectedAction, attribute.Arguments[1]);
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
                .Returns(OperationResult<DtoValidatedSession>.IsFailed(
                    "ACT_SES_01",
                    nameof(IPasswordActivationService.ValidarSessionToken),
                    "Sesión temporal no encontrada.",
                    401,
                    default!));

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

            _loginFlowServiceMock
                .Setup(s => s.EjecutarAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.LoginExitoso(
                    OperationResult<DtoAuthenticationResponse>.Ok(authResponse, "EjecutarAsync")));

            // Act
            var response = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _loginFlowServiceMock.Verify(
                s => s.EjecutarAsync(
                    It.Is<AuthRequest>(r => r == request),
                    "192.168.1.100",
                    0.9),
                Times.Once);
        }

        [Fact]
        public async Task Login_WithoutValidatedCaptchaScore_ReturnsServerError()
        {
            var request = new AuthRequest
            {
                TipoDocumento = "CI",
                Documento = "4773331-2",
                Password = "testPassword"
            };
            _httpContext.Items.Clear();

            var response = await _controller.Login(request);

            var result = Assert.IsType<ObjectResult>(response);
            Assert.Equal(500, result.StatusCode);
            var operationResult = Assert.IsType<OperationResult<DtoAuthenticationResponse>>(result.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("AUTH_CAPTCHA_99", operationResult.ErrorCode);
            _loginFlowServiceMock.Verify(
                s => s.EjecutarAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);
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
                Persona = new DtoPersonaAuth { CodigoPersona = 12345 },
                AccessToken = "test-access-token",
                RefreshToken = "test-refresh-token"
            };

            _loginFlowServiceMock
                .Setup(s => s.EjecutarAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.LoginExitoso(
                    OperationResult<DtoAuthenticationResponse>.Ok(authResponse, "EjecutarAsync")));

            // Act
            var response = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
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

            _loginFlowServiceMock
                .Setup(s => s.EjecutarAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.Fallo(
                    OperationResult<DtoAuthenticationResponse>.IsFailed(
                        errorCode: "AUTH_01",
                        originMethod: "EjecutarAsync",
                        message: "Credenciales inv�lidas",
                        httpCode: 401)));

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

            _loginFlowServiceMock
                .Setup(s => s.EjecutarAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.LoginExitoso(
                    OperationResult<DtoAuthenticationResponse>.Ok(null, "EjecutarAsync")));

            // Act
            var response = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Login_WhenRequiresTwoFactor_ReturnsAcceptedWithOperationResult202()
        {
            var request = new AuthRequest
            {
                TipoDocumento = "CI",
                Documento = "4773331-2",
                Password = "testPassword"
            };

            _loginFlowServiceMock
                .Setup(s => s.EjecutarAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.Requiere2FA(
                    OperationResult<DtoLogin2FARequired>.IsSuccess(
                        new DtoLogin2FARequired
                        {
                            SessionId = "2fa-session",
                            MaskedEmail = "t**t@example.com",
                            Message = "Se envio un codigo de verificacion a tu correo electronico."
                        },
                        "EjecutarAsync",
                        "Se requiere verificacion de dos factores.",
                        202)));

            var response = await _controller.Login(request);

            var acceptedResult = Assert.IsType<AcceptedResult>(response);
            var operationResult = Assert.IsType<OperationResult<DtoLogin2FARequired>>(acceptedResult.Value);
            Assert.True(operationResult.Success);
            Assert.Equal(202, operationResult.HttpCode);
            Assert.Equal("2fa-session", operationResult.Data!.SessionId);
        }

        [Fact]
        public async Task ReenviarCodigo2FA_WhenServiceSucceeds_ReturnsOkAndCallsService()
        {
            var request = new DtoReenviarCodigo2FARequest { SessionId = "2fa-session" };
            var serviceResult = OperationResult<DtoLogin2FARequired>.Ok(
                new DtoLogin2FARequired
                {
                    SessionId = request.SessionId,
                    MaskedEmail = "t**t@example.com",
                    Message = "Se reenvió un nuevo código de verificación a tu correo electrónico."
                },
                nameof(IDosFactoresAuthService.ReenviarCodigoAsync));

            _dosFactoresServiceMock
                .Setup(s => s.ReenviarCodigoAsync(request.SessionId))
                .ReturnsAsync(serviceResult);

            var response = await _controller.ReenviarCodigo2FA(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<DtoLogin2FARequired>>(okResult.Value);
            Assert.True(operationResult.Success);
            Assert.Equal(request.SessionId, operationResult.Data!.SessionId);
            _dosFactoresServiceMock.Verify(s => s.ReenviarCodigoAsync(request.SessionId), Times.Once);
        }

        [Theory]
        [InlineData(401)]
        [InlineData(429)]
        public async Task ReenviarCodigo2FA_WhenServiceFails_ReturnsConfiguredStatusCode(int httpCode)
        {
            var request = new DtoReenviarCodigo2FARequest { SessionId = "2fa-session" };
            _dosFactoresServiceMock
                .Setup(s => s.ReenviarCodigoAsync(request.SessionId))
                .ReturnsAsync(OperationResult<DtoLogin2FARequired>.IsFailed(
                    "AUTH_2FA_RESEND_TEST",
                    nameof(IDosFactoresAuthService.ReenviarCodigoAsync),
                    "No se pudo reenviar el código.",
                    httpCode,
                    default!));

            var response = await _controller.ReenviarCodigo2FA(request);

            var result = Assert.IsType<ObjectResult>(response);
            Assert.Equal(httpCode, result.StatusCode);
            var operationResult = Assert.IsType<OperationResult<DtoLogin2FARequired>>(result.Value);
            Assert.False(operationResult.Success);
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
                message: "Refresh token inv�lido",
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

            var headers = new LoginRateLimitHeaders { Limit = 5, Remaining = 0, ResetTime = DateTimeOffset.UtcNow.AddMinutes(15) };

            _loginFlowServiceMock
                .Setup(s => s.EjecutarAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.FalloConRateLimit(
                    OperationResult<DtoAuthenticationResponse>.IsFailed(
                        "LOGIN_RL_01", "EjecutarAsync", "Demasiados intentos fallidos.", 429),
                    headers));

            // Act
            var response = await _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(429, statusCodeResult.StatusCode);
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
                Persona = new DtoPersonaAuth { CodigoPersona = 12345 },
                AccessToken = "access-token",
                RefreshToken = "refresh-token"
            };

            _loginFlowServiceMock
                .Setup(s => s.EjecutarAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.LoginExitoso(
                    OperationResult<DtoAuthenticationResponse>.Ok(authResponse, "EjecutarAsync")));

            // Act
            var response = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
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
            var headers = new LoginRateLimitHeaders { Limit = 5, Remaining = 0, ResetTime = resetTime };

            _loginFlowServiceMock
                .Setup(s => s.EjecutarAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.FalloConRateLimit(
                    OperationResult<DtoAuthenticationResponse>.IsFailed(
                        "LOGIN_RL_01", "EjecutarAsync", "Demasiados intentos fallidos.", 429),
                    headers));

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

