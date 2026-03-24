// File: UnitTesting/Security/ModelBindingErrorLoggingMiddlewareTests.cs
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using WebApiAdmisiones.Security;
using Xunit;

namespace UnitTesting.Security
{
    public class ModelBindingErrorLoggingMiddlewareTests
    {
        private static DefaultHttpContext CreateHttpContext(string? contentType = null, string? body = null)
        {
            var context = new DefaultHttpContext();
            if (contentType != null)
                context.Request.ContentType = contentType;
            if (body != null)
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                context.Request.Body = new MemoryStream(bytes);
                context.Request.ContentLength = bytes.Length;
            }
            else
            {
                context.Request.Body = new MemoryStream();
            }
            // Use a MemoryStream that won't be disposed
            context.Response.Body = new MemoryStream();
            context.Request.Path = "/test";
            return context;
        }

        private static Mock<IWebHostEnvironment> CreateMockEnvironment(string environmentName = "Development")
        {
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns(environmentName);
            return mockEnv;
        }

        private static async Task<string> CaptureResponseBodyAsync(HttpContext context, Func<Task> action)
        {
            // Capture the response body using a persistent MemoryStream
            var captureStream = new MemoryStream();
            var originalBody = context.Response.Body;
            
            try
            {
                // Temporarily replace the response body
                context.Response.Body = captureStream;
                
                // Execute the action
                await action();
                
                // Read the captured response
                captureStream.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(captureStream, leaveOpen: true);
                return await reader.ReadToEndAsync();
            }
            finally
            {
                // Restore original body
                context.Response.Body = originalBody;
            }
        }

        [Fact]
        public async Task Invoke_PassesThroughNon400Response()
        {
            // Arrange
            var logger = new Mock<ILogger<ModelBindingErrorLoggingMiddleware>>();
            var environment = CreateMockEnvironment();
            var context = CreateHttpContext("application/json", "{\"foo\":\"bar\"}");

            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                var responseBytes = Encoding.UTF8.GetBytes("{\"success\":true}");
                ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                return Task.CompletedTask;
            };

            var middleware = new ModelBindingErrorLoggingMiddleware(next, logger.Object, environment.Object);

            // Act
            var response = await CaptureResponseBodyAsync(context, () => middleware.Invoke(context));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Contains("success", response);
        }

        [Fact]
        public async Task Invoke_LogsWarning_WhenResponseIs400()
        {
            // Arrange
            var logger = new Mock<ILogger<ModelBindingErrorLoggingMiddleware>>();
            var environment = CreateMockEnvironment();
            var context = CreateHttpContext("application/json", "{\"foo\":\"bar\"}");

            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                var problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = new { Field = new[] { "Error message" } }
                };
                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(problemDetails));
                ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                ctx.Response.Body.Seek(0, SeekOrigin.Begin);
                return Task.CompletedTask;
            };

            var middleware = new ModelBindingErrorLoggingMiddleware(next, logger.Object, environment.Object);

            // Act
            await CaptureResponseBodyAsync(context, () => middleware.Invoke(context));

            // Assert
            logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DataAnnotation error response")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Invoke_TransformsProblemDetailsToOperationResult_InDevelopment()
        {
            // Arrange
            var logger = new Mock<ILogger<ModelBindingErrorLoggingMiddleware>>();
            var environment = CreateMockEnvironment("Development");
            var context = CreateHttpContext("application/json", "{\"foo\":\"bar\"}");

            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                var problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = new { Field = new[] { "Error message" } }
                };
                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(problemDetails));
                ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                return Task.CompletedTask;
            };

            var middleware = new ModelBindingErrorLoggingMiddleware(next, logger.Object, environment.Object);

            // Act
            var response = await CaptureResponseBodyAsync(context, () => middleware.Invoke(context));

            // Assert
            Assert.Contains("\"success\":", response.ToLower());
            Assert.Contains("MBM_DA_02", response);
        }

        [Fact]
        public async Task Invoke_MasksErrorsInProduction()
        {
            // Arrange
            var logger = new Mock<ILogger<ModelBindingErrorLoggingMiddleware>>();
            var environment = CreateMockEnvironment("Production");
            var context = CreateHttpContext("application/json", "{\"foo\":\"bar\"}");

            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                var problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = new { Field = new[] { "Sensitive error" } }
                };
                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(problemDetails));
                ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                return Task.CompletedTask;
            };

            var middleware = new ModelBindingErrorLoggingMiddleware(next, logger.Object, environment.Object);

            // Act
            var response = await CaptureResponseBodyAsync(context, () => middleware.Invoke(context));

            // Assert
            Assert.Contains("MBM_DA_01", response);
            Assert.Contains("Solicitud denegada", response);
            Assert.DoesNotContain("Sensitive error", response);
        }

        [Fact]
        public async Task Invoke_PreservesOperationResultInProduction()
        {
            // Arrange
            var logger = new Mock<ILogger<ModelBindingErrorLoggingMiddleware>>();
            var environment = CreateMockEnvironment("Production");
            var context = CreateHttpContext("application/json", "{\"foo\":\"bar\"}");

            var operationResult = OperationResult<object>.IsFailed("CUSTOM_01", "TestMethod", "Custom error", 400, null);

            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(operationResult));
                ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                return Task.CompletedTask;
            };

            var middleware = new ModelBindingErrorLoggingMiddleware(next, logger.Object, environment.Object);

            // Act
            var response = await CaptureResponseBodyAsync(context, () => middleware.Invoke(context));

            // Assert
            Assert.Contains("CUSTOM_01", response);
            Assert.Contains("Custom error", response);
        }

        [Fact]
        public async Task Invoke_MasksErrorsInPreproduction()
        {
            // Arrange
            var logger = new Mock<ILogger<ModelBindingErrorLoggingMiddleware>>();
            var environment = CreateMockEnvironment("Preproduction");
            var context = CreateHttpContext("application/json", "{\"foo\":\"bar\"}");

            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                var problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = new { Field = new[] { "Sensitive error" } }
                };
                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(problemDetails));
                ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                return Task.CompletedTask;
            };

            var middleware = new ModelBindingErrorLoggingMiddleware(next, logger.Object, environment.Object);

            // Act
            var response = await CaptureResponseBodyAsync(context, () => middleware.Invoke(context));

            // Assert
            Assert.Contains("MBM_DA_01", response);
            Assert.Contains("Solicitud denegada", response);
            Assert.DoesNotContain("Sensitive error", response);
        }

        [Fact]
        public async Task Invoke_PreservesOperationResultInPreproduction()
        {
            // Arrange
            var logger = new Mock<ILogger<ModelBindingErrorLoggingMiddleware>>();
            var environment = CreateMockEnvironment("Preproduction");
            var context = CreateHttpContext("application/json", "{\"foo\":\"bar\"}");

            var operationResult = OperationResult<object>.IsFailed("CUSTOM_01", "TestMethod", "Custom error", 400, null);

            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(operationResult));
                ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                return Task.CompletedTask;
            };

            var middleware = new ModelBindingErrorLoggingMiddleware(next, logger.Object, environment.Object);

            // Act
            var response = await CaptureResponseBodyAsync(context, () => middleware.Invoke(context));

            // Assert
            Assert.Contains("CUSTOM_01", response);
            Assert.Contains("Custom error", response);
        }

        [Fact]
        public async Task Invoke_TransformsProblemDetailsToOperationResult_InTesting()
        {
            // Arrange
            var logger = new Mock<ILogger<ModelBindingErrorLoggingMiddleware>>();
            var environment = CreateMockEnvironment("Testing");
            var context = CreateHttpContext("application/json", "{\"foo\":\"bar\"}");

            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                var problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = new { Field = new[] { "Error message" } }
                };
                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(problemDetails));
                ctx.Response.Body.Write(responseBytes, 0, responseBytes.Length);
                return Task.CompletedTask;
            };

            var middleware = new ModelBindingErrorLoggingMiddleware(next, logger.Object, environment.Object);

            // Act
            var response = await CaptureResponseBodyAsync(context, () => middleware.Invoke(context));

            // Assert
            Assert.Contains("\"success\":", response.ToLower());
            Assert.Contains("MBM_DA_02", response);
        }

        [Fact]
        public async Task Invoke_RethrowsException_WithoutLoggingWarning()
        {
            // Arrange
            var logger = new Mock<ILogger<ModelBindingErrorLoggingMiddleware>>();
            var environment = CreateMockEnvironment();
            var context = CreateHttpContext("application/json", "{\"foo\":\"bar\"}");

            var exception = new InvalidOperationException("fail");
            RequestDelegate next = ctx => throw exception;
            var middleware = new ModelBindingErrorLoggingMiddleware(next, logger.Object, environment.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await CaptureResponseBodyAsync(context, () => middleware.Invoke(context))
            );

            // Verificar que NO se logueó warning (se eliminó el log redundante)
            // Solo se debe loguear la entrada como Information si no fue logueada antes
            logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }
    }
}
