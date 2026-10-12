using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using VALUM.Application.Tasks.Commands;
using VALUM.Application.Tasks;
using VALUM.Domain.Entities;
using VALUM.Tests.TestHelpers;
using TaskStatus = VALUM.Domain.Enums.TaskStatus;
using Xunit;

namespace VALUM.Tests.Unit.Application.Tasks.Commands
{
    public class UpdateTaskStatusCommandHandlerLoggingTests
    {
        [Fact]
        public async Task Handle_SuccessfulUpdate_LogsInformationWithStatusTransition()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            var testLogger = new TestLogger<UpdateTaskStatusCommandHandler>();
            var handler = new UpdateTaskStatusCommandHandler(mockTaskRepo.Object, testLogger);

            var task = new TaskItem { Id = Guid.NewGuid(), Status = TaskStatus.Todo, IsDeleted = false };
            mockTaskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);
            mockTaskRepo.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

            var command = new UpdateTaskStatusCommand { Id = task.Id, Status = TaskStatus.Done };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            var infoLogs = testLogger.LogEntries.FindAll(l => l.LogLevel == LogLevel.Information);
            Assert.Single(infoLogs);
            Assert.Contains("Task status updated", infoLogs[0].FormattedMessage);
            Assert.Contains("UpdateTaskStatus", infoLogs[0].FormattedMessage);
        }

        [Fact]
        public async Task Handle_RepeatSameStatus_LogsWarningWithBusinessRuleContext()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            var testLogger = new TestLogger<UpdateTaskStatusCommandHandler>();
            var handler = new UpdateTaskStatusCommandHandler(mockTaskRepo.Object, testLogger);

            var task = new TaskItem { Id = Guid.NewGuid(), Status = TaskStatus.Todo, IsDeleted = false };
            mockTaskRepo.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);

            var command = new UpdateTaskStatusCommand { Id = task.Id, Status = TaskStatus.Todo };

            // Act & Assert
            await Assert.ThrowsAsync<VALUM.Domain.Exceptions.DomainException>(() => handler.Handle(command, CancellationToken.None));

            var warningLogs = testLogger.LogEntries.FindAll(l => l.LogLevel == LogLevel.Warning);
            Assert.Single(warningLogs);
            Assert.Contains("Business rule violation", warningLogs[0].FormattedMessage);
            Assert.Contains("UpdateTaskStatus", warningLogs[0].FormattedMessage);
            Assert.Contains("SameStatus", warningLogs[0].FormattedMessage);
        }
    }
}
