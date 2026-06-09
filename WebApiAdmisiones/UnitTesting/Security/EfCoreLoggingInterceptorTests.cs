// File: UnitTesting/Security/EfCoreLoggingInterceptorTests.cs
using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using WebApiAdmisiones.Security.Observability;

namespace UnitTesting.Security
{
    public class EfCoreLoggingInterceptorTests
    {
        private readonly Mock<ILogger<EfCoreLoggingInterceptor>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly EfCoreLoggingInterceptor _interceptor;

        public EfCoreLoggingInterceptorTests()
        {
            _mockLogger = new Mock<ILogger<EfCoreLoggingInterceptor>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _interceptor = new EfCoreLoggingInterceptor(_mockLogger.Object, _mockHttpContextAccessor.Object);
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/test";
            context.Request.Method = "GET";
            return context;
        }

        private static Mock<DbCommand> CreateMockDbCommand(string? commandText)
        {
            var mockCommand = new Mock<DbCommand>();
            mockCommand.SetupGet(c => c.CommandText).Returns(commandText!);
            return mockCommand;
        }

        private static CommandErrorEventData CreateCommandErrorEventData(Exception exception, TimeSpan duration)
        {
            var mockEventData = new Mock<CommandErrorEventData>(
                MockBehavior.Loose,
                null!, // EventDefinitionBase
                null!, // Func<EventDefinitionBase, EventData, string>
                null!, // DbConnection
                null!, // DbCommand
                null!, // String
                null!, // DbContext
                DbCommandMethod.ExecuteReader,
                Guid.NewGuid(),
                Guid.NewGuid(),
                exception,
                false, // async
                false, // logParameterValues
                DateTimeOffset.UtcNow,
                duration,
                CommandSource.LinqQuery);

            mockEventData.SetupGet(e => e.Exception).Returns(exception);
            mockEventData.SetupGet(e => e.Duration).Returns(duration);

            return mockEventData.Object;
        }

        [Fact]
        public void CommandFailed_LogsError_WithCorrectFormat()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var mockCommand = CreateMockDbCommand("SELECT * FROM Users");
            var exception = new InvalidOperationException("Database connection failed");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(150));

            // Act
            _interceptor.CommandFailed(mockCommand.Object, eventData);

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ERROR")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void CommandFailed_MarksDbErrorAsLogged_InHttpContext()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var mockCommand = CreateMockDbCommand("SELECT * FROM Users");
            var exception = new InvalidOperationException("Database error");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(100));

            // Act
            _interceptor.CommandFailed(mockCommand.Object, eventData);

            // Assert
            Assert.True(httpContext.Items.ContainsKey(LoggingHelper.DbErrorLoggedKey));
            Assert.Equal("Database error", httpContext.Items[LoggingHelper.DbErrorLoggedKey]);
        }

        [Fact]
        public async Task CommandFailedAsync_LogsError_WithCorrectFormat()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var mockCommand = CreateMockDbCommand("INSERT INTO Users VALUES (1, 'Test')");
            var exception = new InvalidOperationException("Insert failed");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(200));

            // Act
            await _interceptor.CommandFailedAsync(mockCommand.Object, eventData);

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ERROR")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CommandFailedAsync_MarksDbErrorAsLogged_InHttpContext()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var mockCommand = CreateMockDbCommand("DELETE FROM Users");
            var exception = new InvalidOperationException("Delete failed");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(50));

            // Act
            await _interceptor.CommandFailedAsync(mockCommand.Object, eventData);

            // Assert
            Assert.True(httpContext.Items.ContainsKey(LoggingHelper.DbErrorLoggedKey));
            Assert.Equal("Delete failed", httpContext.Items[LoggingHelper.DbErrorLoggedKey]);
        }

        [Fact]
        public void CommandFailed_HandlesNullHttpContext_Gracefully()
        {
            // Arrange
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            var mockCommand = CreateMockDbCommand("SELECT 1");
            var exception = new InvalidOperationException("Error");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(10));

            // Act & Assert - Should not throw
            var ex = Record.Exception(() => _interceptor.CommandFailed(mockCommand.Object, eventData));
            Assert.Null(ex);

            // Verify logging still occurs
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CommandFailedAsync_HandlesNullHttpContext_Gracefully()
        {
            // Arrange
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            var mockCommand = CreateMockDbCommand("SELECT 1");
            var exception = new InvalidOperationException("Error");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(10));

            // Act & Assert - Should not throw
            var ex = await Record.ExceptionAsync(() => _interceptor.CommandFailedAsync(mockCommand.Object, eventData));
            Assert.Null(ex);
        }

        [Fact]
        public void CommandFailed_TruncatesLongCommandText()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Create a command text longer than 500 characters
            var longCommandText = new string('X', 600);
            var mockCommand = CreateMockDbCommand(longCommandText);
            var exception = new InvalidOperationException("Error");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(10));

            // Act
            _interceptor.CommandFailed(mockCommand.Object, eventData);

            // Assert - Verify the logged message contains truncation indicator
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TRUNCATED")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void CommandFailed_DoesNotTruncateShortCommandText()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var shortCommandText = "SELECT * FROM Users WHERE Id = 1";
            var mockCommand = CreateMockDbCommand(shortCommandText);
            var exception = new InvalidOperationException("Error");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(10));

            // Act
            _interceptor.CommandFailed(mockCommand.Object, eventData);

            // Assert - Verify the logged message contains the full command (no truncation)
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => 
                        v.ToString()!.Contains(shortCommandText) && 
                        !v.ToString()!.Contains("TRUNCATED")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void CommandFailed_LogsExceptionTypeAndMessage()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var mockCommand = CreateMockDbCommand("SELECT 1");
            var exception = new InvalidOperationException("Specific error message");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(10));

            // Act
            _interceptor.CommandFailed(mockCommand.Object, eventData);

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => 
                        v.ToString()!.Contains("InvalidOperationException") && 
                        v.ToString()!.Contains("Specific error message")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void CommandFailed_LogsClassName()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var mockCommand = CreateMockDbCommand("SELECT 1");
            var exception = new InvalidOperationException("Error");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(10));

            // Act
            _interceptor.CommandFailed(mockCommand.Object, eventData);

            // Assert - Verify the class name is included in the log
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("EfCoreLoggingInterceptor")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void CommandFailed_HandlesEmptyCommandText()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var mockCommand = CreateMockDbCommand(string.Empty);
            var exception = new InvalidOperationException("Error");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(10));

            // Act & Assert - Should not throw
            var ex = Record.Exception(() => _interceptor.CommandFailed(mockCommand.Object, eventData));
            Assert.Null(ex);
        }

        [Fact]
        public void CommandFailed_HandlesNullCommandText()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var mockCommand = CreateMockDbCommand(null!);
            var exception = new InvalidOperationException("Error");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(10));

            // Act & Assert - Should not throw
            var ex = Record.Exception(() => _interceptor.CommandFailed(mockCommand.Object, eventData));
            Assert.Null(ex);
        }

        [Fact]
        public async Task CommandFailedAsync_WithCancellationToken_CompletesSuccessfully()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var mockCommand = CreateMockDbCommand("SELECT 1");
            var exception = new InvalidOperationException("Error");
            var eventData = CreateCommandErrorEventData(exception, TimeSpan.FromMilliseconds(10));
            var cancellationToken = new CancellationToken();

            // Act
            await _interceptor.CommandFailedAsync(mockCommand.Object, eventData, cancellationToken);

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
