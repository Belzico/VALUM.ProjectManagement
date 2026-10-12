using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using VALUM.Application.Tasks.Commands;
using VALUM.Application.Tasks;
using VALUM.Domain.Entities;
using VALUM.Tests.TestHelpers;
using Xunit;

namespace VALUM.Tests.Unit.Application.Tasks.Commands
{
    public class DeleteTaskCommandHandlerLoggingTests
    {
        [Fact]
        public async Task Handle_ActualDeletion_LogsInformationWithTaskId()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            var testLogger = new TestLogger<DeleteTaskCommandHandler>();
            var handler = new DeleteTaskCommandHandler(mockTaskRepo.Object, testLogger);

            var task = new TaskItem { Id = Guid.NewGuid(), IsDeleted = false };
            mockTaskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
            mockTaskRepo.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

            var command = new DeleteTaskCommand { Id = task.Id };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Single(testLogger.LogEntries);
            var logEntry = testLogger.LogEntries[0];
            Assert.Equal(LogLevel.Information, logEntry.LogLevel);
            Assert.Contains("Task soft-deleted", logEntry.FormattedMessage);
            Assert.Contains("DeleteTask", logEntry.FormattedMessage);
        }

        [Fact]
        public async Task Handle_IdempotentNoOp_DoesNotLogSuccess()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            var testLogger = new TestLogger<DeleteTaskCommandHandler>();
            var handler = new DeleteTaskCommandHandler(mockTaskRepo.Object, testLogger);

            var task = new TaskItem { Id = Guid.NewGuid(), IsDeleted = true };
            mockTaskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);

            var command = new DeleteTaskCommand { Id = task.Id };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert - No log entry for idempotent no-op
            Assert.Empty(testLogger.LogEntries);
        }
    }
}
