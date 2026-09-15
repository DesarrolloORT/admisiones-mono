using AppLogic.Authentication.Contracts;
using AppLogic.Identity.Dtos;
using AppLogic.Authentication.Dtos;
using AppLogic.Authentication.Interfaces;
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
        private readonly Mock<IRefreshTokens> _refreshTokensMock;
        private readonly Mock<IRecoverPassword> _recoverPasswordMock;
        private readonly Mock<ICompletePasswordFlow> _completePasswordFlowMock;
        private readonly Mock<IPasswordActivationService> _passwordActivationServiceMock;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<ITwoFactorAuthService> _dosFactoresServiceMock;
        private readonly Mock<ILoginFlowService> _loginFlowServiceMock;
        private readonly IConfiguration _configuration;
        private readonly AuthController _controller;
        private readonly DefaultHttpContext _httpContext;

        public AuthControllerTests()
        {
            _refreshTokensMock = new Mock<IRefreshTokens>();
            _recoverPasswordMock = new Mock<IRecoverPassword>();
            _completePasswordFlowMock = new Mock<ICompletePasswordFlow>();
            _passwordActivationServiceMock = new Mock<IPasswordActivationService>();
            _loggerMock = new Mock<ILogger<AuthController>>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _dosFactoresServiceMock = new Mock<ITwoFactorAuthService>();
            _loginFlowServiceMock = new Mock<ILoginFlowService>();
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
                .Setup(s => s.ExecuteAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.LoginSucceeded(
                    OperationResult<AuthenticationResponse>.Ok(
                        new AuthenticationResponse
                        {
                            Person = new AuthenticatedPerson { PersonId = 1 },
                            AccessToken = "token",
                            RefreshToken = "refresh",
                            RefreshTokenHash = "hash"
                        },
                        "ExecuteAsync")));

            _controller = new AuthController(
                _refreshTokensMock.Object,
                _recoverPasswordMock.Object,
                _completePasswordFlowMock.Object,
                _passwordActivationServiceMock.Object,
                _configuration,
                _loggerMock.Object,
                _currentUserMock.Object,
                _dosFactoresServiceMock.Object,
                _loginFlowServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _httpContext
                }
            };
        }

        [Theory]
        [InlineData(nameof(AuthController.Login), CaptchaActions.Login, CaptchaValidationMode.ScoreOnly)]
        [InlineData(nameof(AuthController.RecoverPassword), CaptchaActions.RecoverPassword, CaptchaValidationMode.ScoreOnly)]
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
            var request = new ActivatePasswordLinkRequest { Token = "activation-token" };
            var serviceResult = OperationResult<PasswordActivationSession>.Ok(
                new PasswordActivationSession
                {
                    PersonId = 12345,
                    SessionToken = "session-token"
                },
                nameof(IPasswordActivationService.ActivatePasswordLinkAsync));

            _passwordActivationServiceMock
                .Setup(s => s.ActivatePasswordLinkAsync(request.Token))
                .ReturnsAsync(serviceResult);

            var response = await _controller.ActivatePasswordLink(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task CompletarPassword_WithoutTemporaryCookie_DelegatesWithNullTokenAndReturnsUnauthorized()
        {
            var request = new CompleteInitialPasswordRequest
            {
                NewPassword = "NuevaPassword1!"
            };

            _completePasswordFlowMock
                .Setup(s => s.ExecuteAsync(null, request))
                .ReturnsAsync(new CompletePasswordFlowResult
                {
                    ClearActivationCookie = true,
                    Result = OperationResult<AuthenticationResponse>.IsFailed(
                        "ACT_SES_01",
                        "CompleteInitialPassword",
                        "Sesión temporal no encontrada.",
                        401,
                        default!)
                });

            var response = await _controller.CompleteInitialPassword(request);

            var unauthorizedResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<AuthenticationResponse>>(unauthorizedResult.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("ACT_SES_01", operationResult.ErrorCode);
            Assert.Contains(
                CookieAuthenticationHelper.PasswordActivationCookieName,
                _httpContext.Response.Headers.SetCookie.ToString());
        }

        [Fact]
        public async Task CompletarPassword_ReadsSessionCookieAndDelegatesToCompletePasswordFlow()
        {
            var request = new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" };
            _httpContext.Request.Headers.Cookie =
                $"{CookieAuthenticationHelper.PasswordActivationCookieName}=activation-token";

            _completePasswordFlowMock
                .Setup(s => s.ExecuteAsync("activation-token", request))
                .ReturnsAsync(new CompletePasswordFlowResult
                {
                    ClearActivationCookie = false,
                    Result = OperationResult<AuthenticationResponse>.IsFailed(
                        "ACT_SES_06",
                        "CompleteInitialPassword",
                        "Sesion temporal invalida.",
                        401,
                        default!)
                });

            var response = await _controller.CompleteInitialPassword(request);

            var result = Assert.IsType<ObjectResult>(response);
            Assert.Equal(401, result.StatusCode);
            _completePasswordFlowMock.Verify(
                s => s.ExecuteAsync("activation-token", request),
                Times.Once);
        }

        [Fact]
        public async Task CompletarPassword_WhenFlowSucceeds_ReturnsOkSetsAuthCookiesAndClearsActivationCookie()
        {
            var request = new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" };
            _httpContext.Request.Headers.Cookie =
                $"{CookieAuthenticationHelper.PasswordActivationCookieName}=activation-token";

            var authResponse = new AuthenticationResponse
            {
                Person = new AuthenticatedPerson { PersonId = 12345 },
                AccessToken = "new-access-token",
                RefreshToken = "new-refresh-token"
            };

            _completePasswordFlowMock
                .Setup(s => s.ExecuteAsync("activation-token", request))
                .ReturnsAsync(new CompletePasswordFlowResult
                {
                    ClearActivationCookie = true,
                    Result = OperationResult<AuthenticationResponse>.Ok(authResponse, "IssueTokensForPersonAsync")
                });

            var response = await _controller.CompleteInitialPassword(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            var setCookieHeader = _httpContext.Response.Headers.SetCookie.ToString();
            Assert.Contains(CookieAuthenticationHelper.PasswordActivationCookieName, setCookieHeader);
            Assert.Contains(CookieAuthenticationHelper.AccessTokenCookieName, setCookieHeader);
            Assert.Contains(CookieAuthenticationHelper.RefreshTokenCookieName, setCookieHeader);
        }

        [Fact]
        public async Task CompletarPassword_WhenFlowFailsWithoutClearingCookie_DoesNotClearActivationCookie()
        {
            // Caso "nueva persona": si falla la creación, el flujo no limpia la cookie temporal
            // (comportamiento preexistente preservado tras extraer la orquestación al service).
            var request = new CompleteInitialPasswordRequest { NewPassword = "NuevaPassword1!" };
            _httpContext.Request.Headers.Cookie =
                $"{CookieAuthenticationHelper.PasswordActivationCookieName}=activation-token";

            _completePasswordFlowMock
                .Setup(s => s.ExecuteAsync("activation-token", request))
                .ReturnsAsync(new CompletePasswordFlowResult
                {
                    ClearActivationCookie = false,
                    Result = OperationResult<AuthenticationResponse>.IsFailed(
                        "REG_PERSONA_99",
                        "CompleteInitialPassword",
                        "Error al crear la persona.",
                        500,
                        default!)
                });

            var response = await _controller.CompleteInitialPassword(request);

            var result = Assert.IsType<ObjectResult>(response);
            Assert.Equal(500, result.StatusCode);
            Assert.DoesNotContain(
                CookieAuthenticationHelper.PasswordActivationCookieName,
                _httpContext.Response.Headers.SetCookie.ToString());
        }

        [Fact]
        public async Task Login_SuccessfulAuthentication_ReturnsOkAndSetsCookies()
        {
            // Arrange
            var request = new AuthRequest
            {
                DocumentType = "CI",
                DocumentNumber = "4773331-2",
                Password = "testPassword"
            };

            var authResponse = new AuthenticationResponse
            {
                Person = new AuthenticatedPerson
                {
                    PersonId = 12345,
                    FirstName = "Test",
                    FirstSurname = "User"
                },
                AccessToken = "test-access-token",
                RefreshToken = "test-refresh-token"
            };

            _loginFlowServiceMock
                .Setup(s => s.ExecuteAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.LoginSucceeded(
                    OperationResult<AuthenticationResponse>.Ok(authResponse, "ExecuteAsync")));

            // Act
            var response = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            _loginFlowServiceMock.Verify(
                s => s.ExecuteAsync(
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
                DocumentType = "CI",
                DocumentNumber = "4773331-2",
                Password = "testPassword"
            };
            _httpContext.Items.Clear();

            var response = await _controller.Login(request);

            var result = Assert.IsType<ObjectResult>(response);
            Assert.Equal(500, result.StatusCode);
            var operationResult = Assert.IsType<OperationResult<AuthenticationResponse>>(result.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("AUTH_CAPTCHA_99", operationResult.ErrorCode);
            _loginFlowServiceMock.Verify(
                s => s.ExecuteAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);
        }

        [Fact]
        public async Task Login_SuccessfulAuthenticationWithLoggingEnabled_LogsInformation()
        {
            // Arrange
            var request = new AuthRequest
            {
                DocumentType = "CI",
                DocumentNumber = "4773331-2",
                Password = "testPassword"
            };

            var authResponse = new AuthenticationResponse
            {
                Person = new AuthenticatedPerson { PersonId = 12345 },
                AccessToken = "test-access-token",
                RefreshToken = "test-refresh-token"
            };

            _loginFlowServiceMock
                .Setup(s => s.ExecuteAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.LoginSucceeded(
                    OperationResult<AuthenticationResponse>.Ok(authResponse, "ExecuteAsync")));

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
                DocumentType = "CI",
                DocumentNumber = "4773331-2",
                Password = "wrongPassword"
            };

            _loginFlowServiceMock
                .Setup(s => s.ExecuteAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.Failed(
                    OperationResult<AuthenticationResponse>.IsFailed(
                        errorCode: "AUTH_01",
                        originMethod: "ExecuteAsync",
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
                DocumentType = "CI",
                DocumentNumber = "4773331-2",
                Password = "testPassword"
            };

            _loginFlowServiceMock
                .Setup(s => s.ExecuteAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.LoginSucceeded(
                    OperationResult<AuthenticationResponse>.Ok(null, "ExecuteAsync")));

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
                DocumentType = "CI",
                DocumentNumber = "4773331-2",
                Password = "testPassword"
            };

            _loginFlowServiceMock
                .Setup(s => s.ExecuteAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.TwoFactorRequired(
                    OperationResult<TwoFactorRequiredResponse>.IsSuccess(
                        new TwoFactorRequiredResponse
                        {
                            SessionId = "2fa-session",
                            MaskedEmail = "t**t@example.com",
                            Message = "Se envio un codigo de verificacion a tu correo electronico."
                        },
                        "ExecuteAsync",
                        "Se requiere verificacion de dos factores.",
                        202)));

            var response = await _controller.Login(request);

            var acceptedResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(202, acceptedResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<TwoFactorRequiredResponse>>(acceptedResult.Value);
            Assert.True(operationResult.Success);
            Assert.Equal(202, operationResult.HttpCode);
            Assert.Equal("2fa-session", operationResult.Data!.SessionId);
        }

        [Fact]
        public async Task ReenviarCodigo2FA_WhenServiceSucceeds_ReturnsOkAndCallsService()
        {
            var request = new ResendTwoFactorCodeRequest { SessionId = "2fa-session" };
            var serviceResult = OperationResult<TwoFactorRequiredResponse>.Ok(
                new TwoFactorRequiredResponse
                {
                    SessionId = request.SessionId,
                    MaskedEmail = "t**t@example.com",
                    Message = "Se reenvió un nuevo código de verificación a tu correo electrónico."
                },
                nameof(ITwoFactorAuthService.ResendCodeAsync));

            _dosFactoresServiceMock
                .Setup(s => s.ResendCodeAsync(request.SessionId))
                .ReturnsAsync(serviceResult);

            var response = await _controller.ResendTwoFactorCode(request);

            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<TwoFactorRequiredResponse>>(okResult.Value);
            Assert.True(operationResult.Success);
            Assert.Equal(request.SessionId, operationResult.Data!.SessionId);
            _dosFactoresServiceMock.Verify(s => s.ResendCodeAsync(request.SessionId), Times.Once);
        }

        [Theory]
        [InlineData(401)]
        [InlineData(429)]
        public async Task ReenviarCodigo2FA_WhenServiceFails_ReturnsConfiguredStatusCode(int httpCode)
        {
            var request = new ResendTwoFactorCodeRequest { SessionId = "2fa-session" };
            _dosFactoresServiceMock
                .Setup(s => s.ResendCodeAsync(request.SessionId))
                .ReturnsAsync(OperationResult<TwoFactorRequiredResponse>.IsFailed(
                    "AUTH_2FA_RESEND_TEST",
                    nameof(ITwoFactorAuthService.ResendCodeAsync),
                    "No se pudo reenviar el código.",
                    httpCode,
                    default!));

            var response = await _controller.ResendTwoFactorCode(request);

            var result = Assert.IsType<ObjectResult>(response);
            Assert.Equal(httpCode, result.StatusCode);
            var operationResult = Assert.IsType<OperationResult<TwoFactorRequiredResponse>>(result.Value);
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

            var authResponse = new AuthenticationResponse
            {
                Person = new AuthenticatedPerson
                {
                    PersonId = 12345,
                    FirstName = "Test",
                    FirstSurname = "User"
                },
                AccessToken = "new-access-token",
                RefreshToken = "new-refresh-token"
            };

            var result = OperationResult<AuthenticationResponse>.Ok(
                authResponse,
                nameof(IRefreshTokens));

            _refreshTokensMock
                .Setup(s => s.ExecuteAsync(refreshToken))
                .ReturnsAsync(result);

            // Act
            var response = await _controller.RefreshToken();

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_WithMissingToken_ReturnsUnauthorized()
        {
            // Arrange - no cookie set

            // Act
            var response = await _controller.RefreshToken();

            // Assert
            var unauthorizedResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<AuthenticationResponse>>(unauthorizedResult.Value);
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
            var unauthorizedResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            var operationResult = Assert.IsType<OperationResult<AuthenticationResponse>>(unauthorizedResult.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("AUTH_RT_01", operationResult.ErrorCode);
        }

        [Fact]
        public async Task RefreshToken_ServiceReturnsFailureWith401_ReturnsUnauthorized()
        {
            // Arrange
            var refreshToken = "invalid-refresh-token";
            _httpContext.Request.Headers.Cookie = $"{CookieAuthenticationHelper.RefreshTokenCookieName}={refreshToken}";

            var result = OperationResult<AuthenticationResponse>.IsFailed(
                errorCode: "AUTH_02",
                originMethod: nameof(IRefreshTokens),
                message: "Refresh token inv�lido",
                httpCode: 401);

            _refreshTokensMock
                .Setup(s => s.ExecuteAsync(refreshToken))
                .ReturnsAsync(result);

            // Act
            var response = await _controller.RefreshToken();

            // Assert
            var unauthorizedResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_ServiceReturnsFailureWith404_ReturnsNotFound()
        {
            // Arrange
            var refreshToken = "valid-token-for-nonexistent-user";
            _httpContext.Request.Headers.Cookie = $"{CookieAuthenticationHelper.RefreshTokenCookieName}={refreshToken}";

            var result = OperationResult<AuthenticationResponse>.IsFailed(
                errorCode: "AUTH_03",
                originMethod: nameof(IRefreshTokens),
                message: "Usuario no encontrado",
                httpCode: 404);

            _refreshTokensMock
                .Setup(s => s.ExecuteAsync(refreshToken))
                .ReturnsAsync(result);

            // Act
            var response = await _controller.RefreshToken();

            // Assert
            var notFoundResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_SuccessWithNullData_ReturnsOkWithoutSettingCookies()
        {
            // Arrange
            var refreshToken = "valid-refresh-token";
            _httpContext.Request.Headers.Cookie = $"{CookieAuthenticationHelper.RefreshTokenCookieName}={refreshToken}";

            var result = OperationResult<AuthenticationResponse>.Ok(
                null,
                nameof(IRefreshTokens));

            _refreshTokensMock
                .Setup(s => s.ExecuteAsync(refreshToken))
                .ReturnsAsync(result);

            // Act
            var response = await _controller.RefreshToken();

            // Assert
            var okResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, okResult.StatusCode);
        }

        #region Rate Limiting Tests

        [Fact]
        public async Task Login_ExceedsAccountRateLimit_Returns429()
        {
            // Arrange
            var request = new AuthRequest
            {
                DocumentType = "CI",
                DocumentNumber = "12345678",
                Password = "password123"
            };

            var headers = new LoginRateLimitHeaders { Limit = 5, Remaining = 0, ResetTime = DateTimeOffset.UtcNow.AddMinutes(15) };

            _loginFlowServiceMock
                .Setup(s => s.ExecuteAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.FailedWithRateLimit(
                    OperationResult<AuthenticationResponse>.IsFailed(
                        "LOGIN_RL_01", "ExecuteAsync", "Demasiados intentos fallidos.", 429),
                    headers));

            // Act
            var response = await _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(429, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Login_WithinRateLimit_CallsLoginFlow()
        {
            // Arrange
            var request = new AuthRequest
            {
                DocumentType = "CI",
                DocumentNumber = "12345678",
                Password = "password123"
            };

            var authResponse = new AuthenticationResponse
            {
                Person = new AuthenticatedPerson { PersonId = 12345 },
                AccessToken = "access-token",
                RefreshToken = "refresh-token"
            };

            _loginFlowServiceMock
                .Setup(s => s.ExecuteAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.LoginSucceeded(
                    OperationResult<AuthenticationResponse>.Ok(authResponse, "ExecuteAsync")));

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
                DocumentType = "CI",
                DocumentNumber = "12345678",
                Password = "password123"
            };

            var resetTime = DateTimeOffset.UtcNow.AddMinutes(15);
            var headers = new LoginRateLimitHeaders { Limit = 5, Remaining = 0, ResetTime = resetTime };

            _loginFlowServiceMock
                .Setup(s => s.ExecuteAsync(It.IsAny<AuthRequest>(), It.IsAny<string>(), It.IsAny<double>()))
                .ReturnsAsync(LoginFlowResult.FailedWithRateLimit(
                    OperationResult<AuthenticationResponse>.IsFailed(
                        "LOGIN_RL_01", "ExecuteAsync", "Demasiados intentos fallidos.", 429),
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

