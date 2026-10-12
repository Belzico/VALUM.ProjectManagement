using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VALUM.Domain.Exceptions;
using VALUM.Application.Exceptions;
using VALUM.Domain.Enums;

namespace VALUM.Application.Tasks.Commands
{
    public class UpdateTaskStatusCommandHandler : IRequestHandler<UpdateTaskStatusCommand>
    {
        private readonly ITaskRepository _taskRepository;

        public UpdateTaskStatusCommandHandler(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public async Task Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.Id);
            
            if (task == null || task.IsDeleted)
            {
                throw new NotFoundException();
            }

            // RN-03: Repeating the current status must return 422
            if (task.Status == request.Status)
            {
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
        }
    }
}
