using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VALUM.Api.Middleware;
using VALUM.Application.Exceptions;
using VALUM.Domain.Exceptions;
using VALUM.Tests.TestHelpers;
using ValidationException = VALUM.Application.Exceptions.ValidationException;
using Xunit;

namespace VALUM.Tests.Unit.Api.Middleware
{
    public class ExceptionHandlingMiddlewareLoggingTests
    {
        [Fact]
        public async Task ValidationException_LogsWarningWithFieldNames()
        {
            // Arrange
            var testLogger = new TestLogger<ExceptionHandlingMiddleware>();
            var middleware = new ExceptionHandlingMiddleware(
                next: (innerHttpContext) => throw new ValidationException(new[] {
                    new FluentValidation.Results.ValidationFailure("Title", "Title is required")
                }),
                testLogger);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.Invoke(context);

            // Assert
            var warningLogs = testLogger.LogEntries.FindAll(l => l.LogLevel == LogLevel.Warning);
            Assert.NotEmpty(warningLogs);
            Assert.Contains("Validation failed", warningLogs[0].FormattedMessage);
        }

        [Fact]
        public async Task NotFoundException_LogsWarningWithResourceType()
        {
            // Arrange
            var testLogger = new TestLogger<ExceptionHandlingMiddleware>();
            var middleware = new ExceptionHandlingMiddleware(
                next: (innerHttpContext) => throw new NotFoundException("Project not found"),
                testLogger);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.Invoke(context);

            // Assert
            var warningLogs = testLogger.LogEntries.FindAll(l => l.LogLevel == LogLevel.Warning);
            Assert.NotEmpty(warningLogs);
            Assert.Contains("Resource not found", warningLogs[0].FormattedMessage);
        }

        [Fact]
        public async Task DomainException_LogsWarningWithRule()
        {
            // Arrange
            var testLogger = new TestLogger<ExceptionHandlingMiddleware>();
            var middleware = new ExceptionHandlingMiddleware(
                next: (innerHttpContext) => throw new DomainException("Cannot archive project with tasks in progress"),
                testLogger);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.Invoke(context);

            // Assert
            var warningLogs = testLogger.LogEntries.FindAll(l => l.LogLevel == LogLevel.Warning);
            Assert.NotEmpty(warningLogs);
            Assert.Contains("Business rule violation", warningLogs[0].FormattedMessage);
        }

        [Fact]
        public async Task UnexpectedException_LogsErrorWithException()
        {
            // Arrange
            var testLogger = new TestLogger<ExceptionHandlingMiddleware>();
            var middleware = new ExceptionHandlingMiddleware(
                next: (innerHttpContext) => throw new InvalidOperationException("Unexpected error"),
                testLogger);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.Invoke(context);

            // Assert
            var errorLogs = testLogger.LogEntries.FindAll(l => l.LogLevel == LogLevel.Error);
            Assert.NotEmpty(errorLogs);
            Assert.NotNull(errorLogs[0].Exception);
        }
    }
}
