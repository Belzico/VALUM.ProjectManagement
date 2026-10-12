using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VALUM.Application.Exceptions;
using VALUM.Application.Projects.Commands;
using VALUM.Application.Projects;
using VALUM.Application.Tasks;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using Xunit;

namespace VALUM.Tests.Unit.Application.Projects.Commands
{
    public class ArchiveProjectCommandHandlerTests
    {
        [Fact]
        public async Task Handle_NonexistentProject_ThrowsNotFoundException()
        {
            // Arrange
            var mockProjectRepo = new Mock<IProjectRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            
            mockProjectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Project?)null);
            
            var handler = new ArchiveProjectCommandHandler(mockProjectRepo.Object, mockTaskRepo.Object, NullLogger<ArchiveProjectCommandHandler>.Instance);
            var command = new ArchiveProjectCommand { Id = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
            
            mockProjectRepo.Verify(r => r.GetByIdAsync(command.Id), Times.Once);
            mockProjectRepo.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task Handle_HasActiveInProgressTasks_ThrowsDomainException()
        {
            // Arrange
            var mockProjectRepo = new Mock<IProjectRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            
            var project = new Project { Id = Guid.NewGuid(), Name = "Test", Status = ProjectStatus.Active };
            mockProjectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(project);
            mockTaskRepo.Setup(r => r.HasActiveInProgressTasksAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            
            var handler = new ArchiveProjectCommandHandler(mockProjectRepo.Object, mockTaskRepo.Object, NullLogger<ArchiveProjectCommandHandler>.Instance);
            var command = new ArchiveProjectCommand { Id = project.Id };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<VALUM.Domain.Exceptions.DomainException>(() => handler.Handle(command, CancellationToken.None));
            Assert.Contains("Cannot archive a project with tasks in progress", ex.Message);
            
            mockProjectRepo.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidActiveProject_ArchivesSuccessfully()
        {
            // Arrange
            var mockProjectRepo = new Mock<IProjectRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            
            var project = new Project { Id = Guid.NewGuid(), Name = "Test", Status = ProjectStatus.Active };
            mockProjectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(project);
            mockTaskRepo.Setup(r => r.HasActiveInProgressTasksAsync(It.IsAny<Guid>())).ReturnsAsync(false);
            mockProjectRepo.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
            
            var handler = new ArchiveProjectCommandHandler(mockProjectRepo.Object, mockTaskRepo.Object, NullLogger<ArchiveProjectCommandHandler>.Instance);
            var command = new ArchiveProjectCommand { Id = project.Id };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ProjectStatus.Archived, project.Status);
            mockProjectRepo.Verify(r => r.UpdateAsync(project), Times.Once);
        }

        [Fact]
        public async Task Handle_AlreadyArchivedProject_RemainsArchived()
        {
            // Arrange
            var mockProjectRepo = new Mock<IProjectRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            
            var project = new Project { Id = Guid.NewGuid(), Name = "Test", Status = ProjectStatus.Archived };
            mockProjectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(project);
            mockTaskRepo.Setup(r => r.HasActiveInProgressTasksAsync(It.IsAny<Guid>())).ReturnsAsync(false);
            mockProjectRepo.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
            
            var handler = new ArchiveProjectCommandHandler(mockProjectRepo.Object, mockTaskRepo.Object, NullLogger<ArchiveProjectCommandHandler>.Instance);
            var command = new ArchiveProjectCommand { Id = project.Id };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ProjectStatus.Archived, project.Status);
            // IMPORTANT: UpdateAsync should NOT be called for already-archived projects (idempotent early return)
            mockProjectRepo.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AlreadyArchivedProjectWithInProgressTask_Returns204()
        {
            // Arrange
            var mockProjectRepo = new Mock<IProjectRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            
            var project = new Project { Id = Guid.NewGuid(), Name = "Test", Status = ProjectStatus.Archived };
            mockProjectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(project);
            // Even if InProgress tasks exist, archived projects should return 204 (idempotent)
            mockTaskRepo.Setup(r => r.HasActiveInProgressTasksAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            mockProjectRepo.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
            
            var handler = new ArchiveProjectCommandHandler(mockProjectRepo.Object, mockTaskRepo.Object, NullLogger<ArchiveProjectCommandHandler>.Instance);
            var command = new ArchiveProjectCommand { Id = project.Id };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ProjectStatus.Archived, project.Status);
            // UpdateAsync should NOT be called (early return before RN-02 check)
            mockProjectRepo.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
            // HasActiveInProgressTasksAsync should NOT be called (early return before RN-02 check)
            mockTaskRepo.Verify(r => r.HasActiveInProgressTasksAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}
