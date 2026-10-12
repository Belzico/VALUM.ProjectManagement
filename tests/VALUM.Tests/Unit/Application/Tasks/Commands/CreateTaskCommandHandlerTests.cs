using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;
using VALUM.Application.Exceptions;
using VALUM.Application.Tasks.Commands;
using VALUM.Application.Tasks;
using VALUM.Application.Projects;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using Xunit;

namespace VALUM.Tests.Unit.Application.Tasks.Commands
{
    public class CreateTaskCommandHandlerTests
    {
        [Fact]
        public async Task Handle_NonexistentProject_ThrowsNotFoundException()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockProjectRepo = new Mock<IProjectRepository>();
            
            mockProjectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Project?)null);
            
            var handler = new CreateTaskCommandHandler(mockTaskRepo.Object, mockProjectRepo.Object);
            var command = new CreateTaskCommand { ProjectId = Guid.NewGuid(), Title = "Test", Priority = TaskPriority.Medium };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ArchivedProject_ThrowsDomainException()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockProjectRepo = new Mock<IProjectRepository>();
            
            var project = new Project { Id = Guid.NewGuid(), Status = ProjectStatus.Archived };
            mockProjectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(project);
            mockTaskRepo.Setup(r => r.IsProjectArchivedAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            
            var handler = new CreateTaskCommandHandler(mockTaskRepo.Object, mockProjectRepo.Object);
            var command = new CreateTaskCommand { ProjectId = project.Id, Title = "Test", Priority = TaskPriority.Medium };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<VALUM.Domain.Exceptions.DomainException>(() => handler.Handle(command, CancellationToken.None));
            Assert.Contains("Cannot create a task under an archived project", ex.Message);
        }
    }
}
