using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using VALUM.Application.Projects.Commands;
using VALUM.Application.Projects;
using VALUM.Application.Tasks;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using VALUM.Tests.TestHelpers;
using Xunit;

namespace VALUM.Tests.Unit.Application.Projects.Commands
{
    public class ArchiveProjectCommandHandlerLoggingTests
    {
        [Fact]
        public async Task Handle_ActualArchive_LogsInformationWithProjectId()
        {
            // Arrange
            var mockProjectRepo = new Mock<IProjectRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            var testLogger = new TestLogger<ArchiveProjectCommandHandler>();
            var handler = new ArchiveProjectCommandHandler(mockProjectRepo.Object, mockTaskRepo.Object, testLogger);

            var project = new Project { Id = Guid.NewGuid(), Status = ProjectStatus.Active };
            mockProjectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);
            mockTaskRepo.Setup(r => r.HasActiveInProgressTasksAsync(project.Id)).ReturnsAsync(false);
            mockProjectRepo.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

            var command = new ArchiveProjectCommand { Id = project.Id };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            var infoLogs = testLogger.LogEntries.FindAll(l => l.LogLevel == LogLevel.Information);
            Assert.Single(infoLogs);
            Assert.Contains("Project archived", infoLogs[0].FormattedMessage);
            Assert.Contains("ArchiveProject", infoLogs[0].FormattedMessage);
        }

        [Fact]
        public async Task Handle_AlreadyArchived_DoesNotLogSuccess()
        {
            // Arrange
            var mockProjectRepo = new Mock<IProjectRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            var testLogger = new TestLogger<ArchiveProjectCommandHandler>();
            var handler = new ArchiveProjectCommandHandler(mockProjectRepo.Object, mockTaskRepo.Object, testLogger);

            var project = new Project { Id = Guid.NewGuid(), Status = ProjectStatus.Archived };
            mockProjectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);

            var command = new ArchiveProjectCommand { Id = project.Id };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert - No log entry for idempotent no-op
            Assert.Empty(testLogger.LogEntries);
        }

        [Fact]
        public async Task Handle_HasActiveInProgressTasks_LogsWarningWithBusinessRuleContext()
        {
            // Arrange
            var mockProjectRepo = new Mock<IProjectRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            var testLogger = new TestLogger<ArchiveProjectCommandHandler>();
            var handler = new ArchiveProjectCommandHandler(mockProjectRepo.Object, mockTaskRepo.Object, testLogger);

            var project = new Project { Id = Guid.NewGuid(), Status = ProjectStatus.Active };
            mockProjectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);
            mockTaskRepo.Setup(r => r.HasActiveInProgressTasksAsync(project.Id)).ReturnsAsync(true);

            var command = new ArchiveProjectCommand { Id = project.Id };

            // Act & Assert
            await Assert.ThrowsAsync<VALUM.Domain.Exceptions.DomainException>(() => handler.Handle(command, CancellationToken.None));

            var warningLogs = testLogger.LogEntries.FindAll(l => l.LogLevel == LogLevel.Warning);
            Assert.Single(warningLogs);
            Assert.Contains("Business rule violation", warningLogs[0].FormattedMessage);
            Assert.Contains("ArchiveProject", warningLogs[0].FormattedMessage);
            Assert.Contains("TasksInProgress", warningLogs[0].FormattedMessage);
        }
    }
}
