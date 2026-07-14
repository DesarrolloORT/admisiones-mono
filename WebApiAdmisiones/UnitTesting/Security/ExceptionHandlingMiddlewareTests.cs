using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Utilities;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;
using WebApiAdmisiones.Security.RequestValidation;
using WebApiAdmisiones.Security.Observability;
using WebApiAdmisiones.Security.Middleware;

namespace UnitTesting.Security
{
    public class ExceptionHandlingMiddlewareTests
    {
        private static DefaultHttpContext CreateContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            
            // Agregar claims para pruebas de logging
            var claims = new List<Claim>
            {
                new Claim("sub", "12345"),
                new Claim(ClaimTypes.Name, "Test User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            context.User = new ClaimsPrincipal(identity);
            
            return context;
        }

        private static async Task<string> GetResponseBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(response.Body, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        [Fact]
        public async Task Invoke_WhenNoException_ResponseNotModified()
        {
            // Arrange
            var context = CreateContext();

            var middleware = new ExceptionHandlingMiddleware(
                _ =>
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    return Task.CompletedTask;
                },
                new FakeLogger<ExceptionHandlingMiddleware>(),
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);
            var responseBody = await GetResponseBody(context.Response);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal(string.Empty, responseBody);
        }

        [Fact]
        public async Task Invoke_WhenInputSanitizationException_ReturnsConflict()
        {
            // Arrange
            var context = CreateContext();
            var logger = new FakeLogger<ExceptionHandlingMiddleware>();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InputSanitizationException("Invalid input"),
                logger,
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);
            var responseBody = await GetResponseBody(context.Response);

            // Assert
            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
            Assert.StartsWith("application/json", context.Response.ContentType);
            Assert.Contains("SANITIZATION_ERROR", responseBody);
            Assert.True(logger.ErrorLogged);
        }

        [Fact]
        public async Task Invoke_WhenInputSanitizationExceptionInDevelopment_IncludesException()
        {
            // Arrange
            var context = CreateContext();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InputSanitizationException("Invalid input"),
                new FakeLogger<ExceptionHandlingMiddleware>(),
                new FakeEnvironment(isDevelopment: true)
            );

            // Act
            await middleware.Invoke(context);
            var responseBody = await GetResponseBody(context.Response);

            // Assert
            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
            Assert.Contains("SANITIZATION_ERROR", responseBody);
            Assert.Contains("Invalid input", responseBody);
        }

        [Fact]
        public async Task Invoke_WhenUnauthorizedAccessException_ReturnsUnauthorized()
        {
            // Arrange
            var context = CreateContext();
            var logger = new FakeLogger<ExceptionHandlingMiddleware>();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new UnauthorizedAccessException("UserId is not available."),
                logger,
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);
            var responseBody = await GetResponseBody(context.Response);

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
            Assert.StartsWith("application/json", context.Response.ContentType);
            Assert.Contains("AUTH_UNAUTHORIZED", responseBody);
        }

        [Fact]
        public async Task Invoke_WhenGeneralException_ReturnsInternalServerError()
        {
            // Arrange
            var context = CreateContext();
            var logger = new FakeLogger<ExceptionHandlingMiddleware>();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("Something went wrong"),
                logger,
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);
            var responseBody = await GetResponseBody(context.Response);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.StartsWith("application/json", context.Response.ContentType);
            Assert.Contains("INTERNAL_ERROR", responseBody);
            Assert.True(logger.ErrorLogged);
        }

        [Fact]
        public async Task Invoke_WhenGeneralExceptionInProduction_ExcludesExceptionDetails()
        {
            // Arrange
            var context = CreateContext();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("Something went wrong"),
                new FakeLogger<ExceptionHandlingMiddleware>(),
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);
            var responseBody = await GetResponseBody(context.Response);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Contains("INTERNAL_ERROR", responseBody);
            // En producción, no debería contener details sensibles
            Assert.DoesNotContain("Something went wrong", responseBody);
        }

        [Fact]
        public async Task Invoke_WhenGeneralExceptionInDevelopment_IncludesExceptionDetails()
        {
            // Arrange
            var context = CreateContext();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("Something went wrong"),
                new FakeLogger<ExceptionHandlingMiddleware>(),
                new FakeEnvironment(isDevelopment: true)
            );

            // Act
            await middleware.Invoke(context);
            var responseBody = await GetResponseBody(context.Response);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Contains("INTERNAL_ERROR", responseBody);
            // En desarrollo, debería incluir el mensaje de excepción
            Assert.Contains("Something went wrong", responseBody);
        }

        [Fact]
        public async Task Invoke_WithUserClaims_LogsCodigoPersona()
        {
            // Arrange
            var context = CreateContext();
            var logger = new FakeLogger<ExceptionHandlingMiddleware>();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("Test exception"),
                logger,
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(logger.ErrorLogged);
            Assert.NotNull(logger.LastLoggedState);
        }

        [Fact]
        public async Task Invoke_WhenDbErrorAlreadyLogged_LogsSummaryOnly()
        {
            // Arrange
            var context = CreateContext();
            var logger = new FakeLogger<ExceptionHandlingMiddleware>();
            
            // Simular que EfCoreLoggingInterceptor ya logueó el error
            context.Items[LoggingHelper.DbErrorLoggedKey] = "SQL Error: Connection failed";

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("DB connection error"),
                logger,
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);
            var responseBody = await GetResponseBody(context.Response);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Contains("INTERNAL_ERROR", responseBody);
            Assert.True(logger.ErrorLogged);
        }

        [Fact]
        public async Task Invoke_WithInnerException_LogsFullExceptionChain()
        {
            // Arrange
            var context = CreateContext();
            var logger = new FakeLogger<ExceptionHandlingMiddleware>();
            
            var innerException = new InvalidOperationException("Inner error");
            var outerException = new ApplicationException("Outer error", innerException);

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw outerException,
                logger,
                new FakeEnvironment(isDevelopment: true)
            );

            // Act
            await middleware.Invoke(context);
            var responseBody = await GetResponseBody(context.Response);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.True(logger.ErrorLogged);
            // En desarrollo, debería incluir el mensaje de la excepción externa
            Assert.Contains("Outer error", responseBody);
        }

        [Fact]
        public async Task Invoke_SetsCorrelationId_InHttpContext()
        {
            // Arrange
            var context = CreateContext();
            var logger = new FakeLogger<ExceptionHandlingMiddleware>();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("Test"),
                logger,
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(context.Items.ContainsKey(LoggingHelper.CorrelationIdKey));
            Assert.IsType<Guid>(context.Items[LoggingHelper.CorrelationIdKey]);
        }

        [Fact]
        public async Task Invoke_PreservesExistingCorrelationId()
        {
            // Arrange
            var context = CreateContext();
            var existingCorrelationId = Guid.NewGuid();
            context.Items[LoggingHelper.CorrelationIdKey] = existingCorrelationId;
            
            var logger = new FakeLogger<ExceptionHandlingMiddleware>();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("Test"),
                logger,
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(existingCorrelationId, context.Items[LoggingHelper.CorrelationIdKey]);
        }

        [Fact]
        public async Task Invoke_WhenInputSanitizationExceptionInProduction_ExcludesExceptionDetails()
        {
            // Arrange
            var context = CreateContext();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InputSanitizationException("Sensitive sanitization error"),
                new FakeLogger<ExceptionHandlingMiddleware>(),
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);
            var responseBody = await GetResponseBody(context.Response);

            // Assert
            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
            Assert.Contains("SANITIZATION_ERROR", responseBody);
            // En producción, no debería contener detalles sensibles
            Assert.DoesNotContain("Sensitive sanitization error", responseBody);
        }

        [Fact]
        public async Task Invoke_WhenNoException_DoesNotLogError()
        {
            // Arrange
            var context = CreateContext();
            var logger = new FakeLogger<ExceptionHandlingMiddleware>();

            var middleware = new ExceptionHandlingMiddleware(
                _ =>
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    return Task.CompletedTask;
                },
                logger,
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.False(logger.ErrorLogged);
            Assert.False(logger.WarningLogged);
        }

        [Fact]
        public async Task Invoke_SetsJsonContentType_OnException()
        {
            // Arrange
            var context = CreateContext();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("Test"),
                new FakeLogger<ExceptionHandlingMiddleware>(),
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);

        // Assert
            Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        }

        [Fact]
        public async Task Invoke_ReturnsValidJsonResponse_OnException()
        {
            // Arrange
            var context = CreateContext();

            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("Test"),
                new FakeLogger<ExceptionHandlingMiddleware>(),
                new FakeEnvironment(isDevelopment: false)
            );

            // Act
            await middleware.Invoke(context);
            var responseBody = await GetResponseBody(context.Response);

            // Assert
            var jsonDoc = JsonDocument.Parse(responseBody);
            Assert.NotNull(jsonDoc);
            Assert.True(jsonDoc.RootElement.TryGetProperty("success", out _) || 
                        jsonDoc.RootElement.TryGetProperty("Success", out _));
        }

        // Fake logger que registra si se logueó
        private class FakeLogger<T> : ILogger<T>
        {
            public bool ErrorLogged { get; private set; }
            public bool WarningLogged { get; private set; }
            public object? LastLoggedState { get; private set; }

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => true;
            
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception, Func<TState, System.Exception?, string> formatter)
            {
                LastLoggedState = state;
                if (logLevel == LogLevel.Error)
                    ErrorLogged = true;
                if (logLevel == LogLevel.Warning)
                    WarningLogged = true;
            }

            private class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new NullScope();
                public void Dispose() { }
            }
        }

        // Fake environment que permite controlar IsDevelopment
        private class FakeEnvironment : IHostEnvironment
        {
            private readonly bool _isDevelopment;

            public FakeEnvironment(bool isDevelopment)
            {
                _isDevelopment = isDevelopment;
                EnvironmentName = isDevelopment ? "Development" : "Production";
            }
            
            public string EnvironmentName { get; set; }
            public string ApplicationName { get; set; } = "TestApp";
            public string ContentRootPath { get; set; } = ".";
            public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            
            public bool IsDevelopment() => _isDevelopment;
        }
    }
}
