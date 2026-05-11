using AppLogic.IServices;
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
            var filter = new RequireCaptchaFilter(recaptchaMock.Object);
            var nextCalled = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            Assert.True(nextCalled);
            Assert.Null(context.Result);
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
            var filter = new RequireCaptchaFilter(recaptchaMock.Object);
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

        private static ActionExecutedContext CreateExecutedContext(ActionExecutingContext context)
        {
            return new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                context.Controller);
        }
    }
}
