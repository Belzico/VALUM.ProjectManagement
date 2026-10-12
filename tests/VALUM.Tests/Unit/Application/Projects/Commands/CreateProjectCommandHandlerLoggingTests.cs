using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using VALUM.Application.Projects.Commands;
using VALUM.Application.Projects;
using VALUM.Domain.Entities;
using VALUM.Tests.TestHelpers;
using Xunit;

namespace VALUM.Tests.Unit.Application.Projects.Commands
{
    public class CreateProjectCommandHandlerLoggingTests
    {
        [Fact]
        public async Task Handle_SuccessfulCreation_LogsInformationWithProjectId()
        {
            // Arrange
            var mockRepo = new Mock<IProjectRepository>();
            var testLogger = new TestLogger<CreateProjectCommandHandler>();
            var handler = new CreateProjectCommandHandler(mockRepo.Object, testLogger);

            var command = new CreateProjectCommand
            {
                Name = "Test Project",
                Description = "Test Description",
                OwnerId = Guid.NewGuid()
            };

            mockRepo.Setup(r => r.AddAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Single(testLogger.LogEntries);
            var logEntry = testLogger.LogEntries[0];
            Assert.Equal(LogLevel.Information, logEntry.LogLevel);
            Assert.Contains("Project created", logEntry.FormattedMessage);
            Assert.Contains("CreateProject", logEntry.FormattedMessage);
        }
    }
}
