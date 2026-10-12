using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VALUM.Application.Exceptions;
using VALUM.Application.Tasks.Commands;
using VALUM.Application.Tasks;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using Xunit;

namespace VALUM.Tests.Unit.Application.Tasks.Commands
{
    public class UpdateTaskStatusCommandHandlerTests
    {
        [Fact]
        public async Task Handle_NonexistentTask_ThrowsNotFoundException()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            
            mockTaskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);
            
            var handler = new UpdateTaskStatusCommandHandler(mockTaskRepo.Object, NullLogger<UpdateTaskStatusCommandHandler>.Instance);
            var command = new UpdateTaskStatusCommand { Id = Guid.NewGuid(), Status = VALUM.Domain.Enums.TaskStatus.Done };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_SoftDeletedTask_ThrowsNotFoundException()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            
            var task = new TaskItem { Id = Guid.NewGuid(), IsDeleted = true };
            mockTaskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);
            
            var handler = new UpdateTaskStatusCommandHandler(mockTaskRepo.Object, NullLogger<UpdateTaskStatusCommandHandler>.Instance);
            var command = new UpdateTaskStatusCommand { Id = task.Id, Status = VALUM.Domain.Enums.TaskStatus.Done };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_RepeatSameStatus_ThrowsDomainException()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            
            var task = new TaskItem { Id = Guid.NewGuid(), Status = VALUM.Domain.Enums.TaskStatus.Todo, IsDeleted = false };
            mockTaskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);
            mockTaskRepo.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);
            
            var handler = new UpdateTaskStatusCommandHandler(mockTaskRepo.Object, NullLogger<UpdateTaskStatusCommandHandler>.Instance);
            var command = new UpdateTaskStatusCommand { Id = task.Id, Status = VALUM.Domain.Enums.TaskStatus.Todo };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<VALUM.Domain.Exceptions.DomainException>(() => handler.Handle(command, CancellationToken.None));
            Assert.Contains("Task is already in the requested status", ex.Message);
        }

        [Fact]
        public async Task Handle_TransitionToDone_SetsCompletedAt()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            
            var task = new TaskItem { Id = Guid.NewGuid(), Status = VALUM.Domain.Enums.TaskStatus.Todo, IsDeleted = false };
            mockTaskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);
            mockTaskRepo.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);
            
            var handler = new UpdateTaskStatusCommandHandler(mockTaskRepo.Object, NullLogger<UpdateTaskStatusCommandHandler>.Instance);
            var command = new UpdateTaskStatusCommand { Id = task.Id, Status = VALUM.Domain.Enums.TaskStatus.Done };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(VALUM.Domain.Enums.TaskStatus.Done, task.Status);
            Assert.NotNull(task.CompletedAt);
        }

        [Fact]
        public async Task Handle_TransitionFromDone_ClearsCompletedAt()
        {
            // Arrange
            var mockTaskRepo = new Mock<ITaskRepository>();
            
            var task = new TaskItem { Id = Guid.NewGuid(), Status = VALUM.Domain.Enums.TaskStatus.Done, CompletedAt = DateTime.UtcNow, IsDeleted = false };
            mockTaskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);
            mockTaskRepo.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);
            
            var handler = new UpdateTaskStatusCommandHandler(mockTaskRepo.Object, NullLogger<UpdateTaskStatusCommandHandler>.Instance);
            var command = new UpdateTaskStatusCommand { Id = task.Id, Status = VALUM.Domain.Enums.TaskStatus.Todo };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(VALUM.Domain.Enums.TaskStatus.Todo, task.Status);
            Assert.Null(task.CompletedAt);
        }
    }
}
