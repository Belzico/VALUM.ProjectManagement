using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VALUM.Domain.Exceptions;
using VALUM.Application.Exceptions;
using VALUM.Domain.Enums;

namespace VALUM.Application.Tasks.Commands
{
    public class UpdateTaskStatusCommandHandler : IRequestHandler<UpdateTaskStatusCommand>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<UpdateTaskStatusCommandHandler> _logger;

        public UpdateTaskStatusCommandHandler(ITaskRepository taskRepository, ILogger<UpdateTaskStatusCommandHandler> logger)
        {
            _taskRepository = taskRepository;
            _logger = logger;
        }

        public async Task Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.Id);
            
            if (task == null || task.IsDeleted)
            {
                throw new NotFoundException();
            }

            var previousStatus = task.Status;

            // RN-03: Repeating the current status must return 422
            if (task.Status == request.Status)
            {
                _logger.LogWarning(
                    "Business rule violation. Operation: {Operation}, ResourceType: {ResourceType}, TaskId: {TaskId}, FailureCategory: {FailureCategory}",
                    "UpdateTaskStatus",
                    "Task",
                    task.Id,
                    "SameStatus");
                throw new DomainException("Task is already in the requested status.");
            }

            // RN-03: When transitioning to Done, set CompletedAt = DateTime.UtcNow
            if (request.Status == VALUM.Domain.Enums.TaskStatus.Done)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            // RN-03: When transitioning from Done to another status, clear CompletedAt
            else if (task.Status == VALUM.Domain.Enums.TaskStatus.Done)
            {
                task.CompletedAt = null;
            }
            // RN-03: When transitioning to InProgress or Todo, CompletedAt should be null
            else
            {
                task.CompletedAt = null;
            }

            task.Status = request.Status;

            await _taskRepository.UpdateAsync(task);

            _logger.LogInformation(
                "Task status updated. Operation: {Operation}, TaskId: {TaskId}, PreviousStatus: {PreviousStatus}, NewStatus: {NewStatus}",
                "UpdateTaskStatus",
                task.Id,
                previousStatus,
                task.Status);
        }
    }
}
