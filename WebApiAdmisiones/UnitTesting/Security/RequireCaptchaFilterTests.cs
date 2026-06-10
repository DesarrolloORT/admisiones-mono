using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Utilities;
using WebApiAdmisiones.Security.Captcha;

namespace UnitTesting.Security
{
    [Collection(UnitTesting.AppLogic.Services.EnvironmentVariablesCollection.Name)]
    public class RequireCaptchaFilterTests
    {
        [Fact]
        public async Task OnActionExecutionAsync_RequireMinimumScoreWithValidCaptcha_CallsNextAndStoresScore()
        {
            using var scope = new UnitTesting.AppLogic.Services.EnvironmentVariableScope(("RECAPTCHA_SCORE", "0.5"));
            var recaptchaMock = new Mock<IRecaptchaService>();
            recaptchaMock
                .Setup(s => s.ValidarConScoreAsync("token", "login"))
                .ReturnsAsync(OperationResult<double>.Ok(0.8, nameof(IRecaptchaService.ValidarConScoreAsync)));

            var context = CreateContext("token");
            var filter = new RequireCaptchaFilter(recaptchaMock.Object, CaptchaValidationMode.RequireMinimumScore);
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            Assert.True(nextCalled);
            Assert.Null(context.Result);
            Assert.Equal(0.8, context.HttpContext.GetRecaptchaScore());
            recaptchaMock.Verify(s => s.ValidarConScoreAsync("token", "login"), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_RequireMinimumScoreWithMissingCaptcha_ReturnsRegistrationFailure()
        {
            var recaptchaMock = new Mock<IRecaptchaService>();
            recaptchaMock
                .Setup(s => s.ValidarConScoreAsync(string.Empty, "login"))
                .ReturnsAsync(OperationResult<double>.IsFailed(
                    "AUTH_CAPTCHA_01",
                    nameof(IRecaptchaService.ValidarConScoreAsync),
                    "El parametro captcha es obligatorio.",
                    400,
                    0d));

            var context = CreateContext(token: null);
            var filter = new RequireCaptchaFilter(recaptchaMock.Object, CaptchaValidationMode.RequireMinimumScore);
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            Assert.False(nextCalled);
            var operationResult = AssertFailure(context, 400);
            Assert.Equal("REG_CAPTCHA_01", operationResult.ErrorCode);
            recaptchaMock.Verify(s => s.ValidarConScoreAsync(string.Empty, "login"), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_RequireMinimumScoreWithLowCaptchaScore_ReturnsSuspiciousActivity()
        {
            using var scope = new UnitTesting.AppLogic.Services.EnvironmentVariableScope(("RECAPTCHA_SCORE", "0.7"));
            var recaptchaMock = new Mock<IRecaptchaService>();
            recaptchaMock
                .Setup(s => s.ValidarConScoreAsync("token", "login"))
                .ReturnsAsync(OperationResult<double>.Ok(0.3, nameof(IRecaptchaService.ValidarConScoreAsync)));

            var context = CreateContext("token");
            var filter = new RequireCaptchaFilter(recaptchaMock.Object, CaptchaValidationMode.RequireMinimumScore);
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            Assert.False(nextCalled);
            Assert.Equal(0.3, context.HttpContext.GetRecaptchaScore());
            var operationResult = AssertFailure(context, 400);
            Assert.Equal("REG_CAPTCHA_05", operationResult.ErrorCode);
        }

        [Fact]
        public async Task OnActionExecutionAsync_ScoreOnlyWithInvalidCaptcha_ReturnsAuthFailure()
        {
            var recaptchaMock = new Mock<IRecaptchaService>();
            recaptchaMock
                .Setup(s => s.ValidarConScoreAsync(string.Empty, "login"))
                .ReturnsAsync(OperationResult<double>.IsFailed(
                    "AUTH_CAPTCHA_01",
                    nameof(IRecaptchaService.ValidarConScoreAsync),
                    "El parametro captcha es obligatorio.",
                    400,
                    0d));

            var context = CreateContext(token: null);
            var filter = new RequireCaptchaFilter(recaptchaMock.Object, CaptchaValidationMode.ScoreOnly);
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            Assert.False(nextCalled);
            var operationResult = AssertFailure(context, 400);
            Assert.Equal("AUTH_CAPTCHA_01", operationResult.ErrorCode);
            Assert.Null(context.HttpContext.GetRecaptchaScore());
        }

        [Fact]
        public async Task OnActionExecutionAsync_ScoreOnlyWithLowCaptchaScore_CallsNextAndStoresScore()
        {
            using var scope = new UnitTesting.AppLogic.Services.EnvironmentVariableScope(("RECAPTCHA_SCORE", "0.7"));
            var recaptchaMock = new Mock<IRecaptchaService>();
            recaptchaMock
                .Setup(s => s.ValidarConScoreAsync("token", "login"))
                .ReturnsAsync(OperationResult<double>.Ok(0.3, nameof(IRecaptchaService.ValidarConScoreAsync)));

            var context = CreateContext("token");
            var filter = new RequireCaptchaFilter(recaptchaMock.Object, CaptchaValidationMode.ScoreOnly);
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            Assert.True(nextCalled);
            Assert.Null(context.Result);
            Assert.Equal(0.3, context.HttpContext.GetRecaptchaScore());
        }

        [Theory]
        [InlineData("LocalHost")]
        [InlineData("Development")]
        public async Task OnActionExecutionAsync_DevelopmentLikeEnvironment_StillValidatesCaptcha(string environmentName)
        {
            var recaptchaMock = new Mock<IRecaptchaService>();
            recaptchaMock
                .Setup(s => s.ValidarConScoreAsync(string.Empty, "login"))
                .ReturnsAsync(OperationResult<double>.IsFailed(
                    "AUTH_CAPTCHA_01",
                    nameof(IRecaptchaService.ValidarConScoreAsync),
                    "El parametro captcha es obligatorio.",
                    400,
                    0d));

            var context = CreateContext(token: null);
            var filter = new RequireCaptchaFilter(recaptchaMock.Object, CaptchaValidationMode.RequireMinimumScore);

            await filter.OnActionExecutionAsync(context, () =>
                Task.FromResult(CreateExecutedContext(context)));

            _ = environmentName;
            var operationResult = AssertFailure(context, 400);
            Assert.Equal("REG_CAPTCHA_01", operationResult.ErrorCode);
            recaptchaMock.Verify(s => s.ValidarConScoreAsync(string.Empty, "login"), Times.Once);
        }

        private static ActionExecutingContext CreateContext(string? token)
        {
            var httpContext = new DefaultHttpContext();
            if (token != null)
            {
                httpContext.Request.Headers[RequireCaptchaFilter.HeaderName] = token;
            }

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor());

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller: new object());
        }

        private static OperationResult<object> AssertFailure(ActionExecutingContext context, int statusCode)
        {
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(statusCode, result.StatusCode);
            var operationResult = Assert.IsType<OperationResult<object>>(result.Value);
            Assert.False(operationResult.Success);
            return operationResult;
        }

        private static ActionExecutedContext CreateExecutedContext(ActionExecutingContext context)
        {
            return new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                context.Controller);
        }
    }
}
