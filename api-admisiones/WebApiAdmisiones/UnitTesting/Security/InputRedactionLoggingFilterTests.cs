using AppLogic.Contracts.Redaction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using WebApiAdmisiones.Security.Observability;
using WebApiAdmisiones.Security.RequestValidation;
using Xunit;

namespace UnitTesting.Security
{
    /// <summary>
    /// El filtro loguea la entrada con los argumentos redactados y marca el request para que el
    /// middleware no vuelva a loguearlo. El atajo GET/HEAD sin argumentos evita el overhead.
    /// </summary>
    public class InputRedactionLoggingFilterTests
    {
        private static ActionExecutingContext CrearContexto(
            string method,
            Dictionary<string, object?>? argumentos = null,
            HttpContext? httpContext = null)
        {
            var context = httpContext ?? new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Path = "/api/test";

            var actionContext = new ActionContext(context, new RouteData(), new ControllerActionDescriptor
            {
                ActionName = "TestAction",
                ControllerName = "Test"
            });

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                argumentos ?? new Dictionary<string, object?>(),
                controller: new object());
        }

        private static Mock<ILogger<InputRedactionLoggingFilter>> LoggerMock(bool enabled = true)
        {
            var mock = new Mock<ILogger<InputRedactionLoggingFilter>>();
            mock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(enabled);
            return mock;
        }

        private static ActionExecutionDelegate Next(Action? onCall = null) => () =>
        {
            onCall?.Invoke();
            return Task.FromResult<ActionExecutedContext>(null!);
        };

        [Fact]
        public async Task OnActionExecutionAsync_GetSinArgumentos_LogueaSinDatosYContinua()
        {
            var loggerMock = LoggerMock();
            var filter = new InputRedactionLoggingFilter(loggerMock.Object);
            var context = CrearContexto(HttpMethods.Get);
            var siguienteEjecutado = false;

            await filter.OnActionExecutionAsync(context, Next(() => siguienteEjecutado = true));

            Assert.True(siguienteEjecutado);
            Assert.True(context.HttpContext.Items.ContainsKey(LoggingHelper.EntradaLoggedKey));
            loggerMock.Verify(l => l.Log(
                LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task OnActionExecutionAsync_HeadSinArgumentos_TomaElMismoAtajo()
        {
            var filter = new InputRedactionLoggingFilter(LoggerMock().Object);
            var context = CrearContexto(HttpMethods.Head);
            var siguienteEjecutado = false;

            await filter.OnActionExecutionAsync(context, Next(() => siguienteEjecutado = true));

            Assert.True(siguienteEjecutado);
        }

        /// <summary>La redacción es dirigida por [Redact] en el DTO, no por el nombre de la propiedad.</summary>
        private sealed class LoginRequestFake
        {
            [Redact]
            public string? Password { get; set; }

            public string? DocumentNumber { get; set; }
        }

        [Fact]
        public async Task OnActionExecutionAsync_PostConArgumentos_LogueaLosArgumentosRedactados()
        {
            var loggerMock = LoggerMock();
            var filter = new InputRedactionLoggingFilter(loggerMock.Object);
            var context = CrearContexto(HttpMethods.Post, new Dictionary<string, object?>
            {
                ["request"] = new LoginRequestFake { Password = "secreto", DocumentNumber = "1234567-2" }
            });
            string? mensajeLogueado = null;
            loggerMock
                .Setup(l => l.Log(
                    LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(new InvocationAction(inv => mensajeLogueado = inv.Arguments[2]?.ToString()));

            await filter.OnActionExecutionAsync(context, Next());

            Assert.NotNull(mensajeLogueado);
            Assert.DoesNotContain("secreto", mensajeLogueado);
            Assert.Contains("1234567-2", mensajeLogueado);
        }

        [Fact]
        public async Task OnActionExecutionAsync_GetConArgumentos_NoTomaElAtajo()
        {
            var filter = new InputRedactionLoggingFilter(LoggerMock().Object);
            var context = CrearContexto(HttpMethods.Get, new Dictionary<string, object?> { ["id"] = 5L });
            var siguienteEjecutado = false;

            await filter.OnActionExecutionAsync(context, Next(() => siguienteEjecutado = true));

            Assert.True(siguienteEjecutado);
            Assert.True(context.HttpContext.Items.ContainsKey(LoggingHelper.EntradaLoggedKey));
        }

        [Fact]
        public async Task OnActionExecutionAsync_LoggingDeshabilitado_NoLogueaPeroContinua()
        {
            var loggerMock = LoggerMock(enabled: false);
            var filter = new InputRedactionLoggingFilter(loggerMock.Object);
            var context = CrearContexto(HttpMethods.Post, new Dictionary<string, object?> { ["id"] = 5L });
            var siguienteEjecutado = false;

            await filter.OnActionExecutionAsync(context, Next(() => siguienteEjecutado = true));

            Assert.True(siguienteEjecutado);
            loggerMock.Verify(l => l.Log(
                It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
        }

        [Fact]
        public async Task OnActionExecutionAsync_ArgumentoNull_NoRompeLaRedaccion()
        {
            var filter = new InputRedactionLoggingFilter(LoggerMock().Object);
            var context = CrearContexto(HttpMethods.Post, new Dictionary<string, object?> { ["request"] = null });

            await filter.OnActionExecutionAsync(context, Next());

            Assert.True(context.HttpContext.Items.ContainsKey(LoggingHelper.EntradaLoggedKey));
        }
    }
}
