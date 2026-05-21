using AppLogic.IServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Utilities;
using WebApiAdmisiones.Security;

namespace UnitTesting.Security
{
    public class RequireCaptchaFilterTests
    {
        [Fact]
        public async Task OnActionExecutionAsync_ValidCaptcha_CallsNext()
        {
            var recaptchaMock = new Mock<IRecaptchaService>();
            recaptchaMock
                .Setup(s => s.ValidarAsync("token"))
                .ReturnsAsync(OperationResult<bool>.Ok(true, nameof(IRecaptchaService.ValidarAsync)));

            var context = CreateContext("token");
            var filter = CreateFilter(recaptchaMock, "Production");
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            Assert.True(nextCalled);
            Assert.Null(context.Result);
            recaptchaMock.Verify(s => s.ValidarAsync("token"), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_InvalidCaptcha_ReturnsFailure()
        {
            var recaptchaMock = new Mock<IRecaptchaService>();
            recaptchaMock
                .Setup(s => s.ValidarAsync(string.Empty))
                .ReturnsAsync(OperationResult<bool>.IsFailed(
                    "REG_CAPTCHA_01",
                    nameof(IRecaptchaService.ValidarAsync),
                    "El parámetro captcha es obligatorio.",
                    400,
                    false));

            var context = CreateContext(token: null);
            var filter = CreateFilter(recaptchaMock, "Production");
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            Assert.False(nextCalled);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(400, result.StatusCode);
            var operationResult = Assert.IsType<OperationResult<object>>(result.Value);
            Assert.False(operationResult.Success);
            Assert.Equal("REG_CAPTCHA_01", operationResult.ErrorCode);
            recaptchaMock.Verify(s => s.ValidarAsync(string.Empty), Times.Once);
        }

        [Theory]
        [InlineData("LocalHost")]
        [InlineData("Development")]
        public async Task OnActionExecutionAsync_DevelopmentLikeEnvironment_SkipsCaptcha(string environmentName)
        {
            var recaptchaMock = new Mock<IRecaptchaService>();
            var context = CreateContext(token: null);
            var filter = CreateFilter(recaptchaMock, environmentName);
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            Assert.True(nextCalled);
            Assert.Null(context.Result);
            recaptchaMock.Verify(s => s.ValidarAsync(It.IsAny<string>()), Times.Never);
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

        private static RequireCaptchaFilter CreateFilter(Mock<IRecaptchaService> recaptchaMock, string environmentName)
        {
            var environmentMock = new Mock<IWebHostEnvironment>();
            environmentMock
                .Setup(e => e.EnvironmentName)
                .Returns(environmentName);

            return new RequireCaptchaFilter(recaptchaMock.Object, environmentMock.Object);
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
