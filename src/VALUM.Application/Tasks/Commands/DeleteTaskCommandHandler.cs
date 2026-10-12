using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VALUM.Application.Exceptions;

namespace VALUM.Application.Tasks.Commands
{
    public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<DeleteTaskCommandHandler> _logger;

        public DeleteTaskCommandHandler(ITaskRepository taskRepository, ILogger<DeleteTaskCommandHandler> logger)
        {
            _taskRepository = taskRepository;
            _logger = logger;
        }

        public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.Id);
            
            // RN-04: Truly nonexistent task => HTTP 404
            if (task == null)
            {
                throw new NotFoundException();
            }

            // RN-04: Soft-delete: set IsDeleted=true
            // RN-04: Already soft-deleted => return 204 (idempotent)
            if (!task.IsDeleted)
            {
                task.IsDeleted = true;
                await _taskRepository.UpdateAsync(task);

                _logger.LogInformation(
                    "Task soft-deleted. Operation: {Operation}, TaskId: {TaskId}",
                    "DeleteTask",
                    task.Id);
            }
            // If already soft-deleted, do nothing (idempotent)
        }
    }
}
