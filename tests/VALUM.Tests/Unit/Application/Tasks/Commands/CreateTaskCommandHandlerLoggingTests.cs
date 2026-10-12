using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using VALUM.Application.Tasks.Commands;
using VALUM.Application.Tasks;
using VALUM.Application.Projects;
using VALUM.Domain.Entities;
using VALUM.Tests.TestHelpers;
using TaskPriority = VALUM.Domain.Enums.TaskPriority;
using ProjectStatus = VALUM.Domain.Enums.ProjectStatus;
using Xunit;

namespace VALUM.Tests.Unit.Application.Tasks.Commands
{
    public class CreateTaskCommandHandlerLoggingTests
    {
        [Fact]
        public async Task Handle_SuccessfulCreation_LogsInformationWithTaskIdAndProjectId()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockProjectRepo = new Mock<IProjectRepository>();
            var testLogger = new TestLogger<CreateTaskCommandHandler>();
            var handler = new CreateTaskCommandHandler(mockTaskRepo.Object, mockProjectRepo.Object, testLogger);

            var projectId = Guid.NewGuid();
            var project = new Project { Id = projectId, Status = ProjectStatus.Active };
            mockProjectRepo.Setup(r => r.GetByIdAsync(projectId)).ReturnsAsync(project);
            mockTaskRepo.Setup(r => r.IsProjectArchivedAsync(projectId)).ReturnsAsync(false);
            mockTaskRepo.Setup(r => r.AddAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

            var command = new CreateTaskCommand
            {
                ProjectId = projectId,
                Title = "Test Task",
                Priority = TaskPriority.Medium
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            var infoLogs = testLogger.LogEntries.FindAll(l => l.LogLevel == LogLevel.Information);
            Assert.Single(infoLogs);
            Assert.Contains("Task created", infoLogs[0].FormattedMessage);
            Assert.Contains("CreateTask", infoLogs[0].FormattedMessage);
        }

        [Fact]
        public async Task Handle_ArchivedProject_LogsWarningWithBusinessRuleContext()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockProjectRepo = new Mock<IProjectRepository>();
            var testLogger = new TestLogger<CreateTaskCommandHandler>();
            var handler = new CreateTaskCommandHandler(mockTaskRepo.Object, mockProjectRepo.Object, testLogger);

            var projectId = Guid.NewGuid();
            var project = new Project { Id = projectId, Status = ProjectStatus.Archived };
            mockProjectRepo.Setup(r => r.GetByIdAsync(projectId)).ReturnsAsync(project);
            mockTaskRepo.Setup(r => r.IsProjectArchivedAsync(projectId)).ReturnsAsync(true);

            var command = new CreateTaskCommand
            {
                ProjectId = projectId,
                Title = "Test Task",
                Priority = TaskPriority.Medium
            };

            // Act & Assert
            await Assert.ThrowsAsync<VALUM.Domain.Exceptions.DomainException>(() => handler.Handle(command, CancellationToken.None));

            var warningLogs = testLogger.LogEntries.FindAll(l => l.LogLevel == LogLevel.Warning);
            Assert.Single(warningLogs);
            Assert.Contains("Business rule violation", warningLogs[0].FormattedMessage);
            Assert.Contains("CreateTask", warningLogs[0].FormattedMessage);
            Assert.Contains("ArchivedProject", warningLogs[0].FormattedMessage);
        }
    }
}
