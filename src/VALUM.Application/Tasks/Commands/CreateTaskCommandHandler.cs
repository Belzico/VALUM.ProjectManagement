using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VALUM.Application.Exceptions;
using VALUM.Application.Tasks.DTOs;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using VALUM.Domain.Exceptions;
using VALUM.Application.Projects;

namespace VALUM.Application.Tasks.Commands
{
    public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<CreateTaskCommandHandler> _logger;

        public CreateTaskCommandHandler(ITaskRepository taskRepository, IProjectRepository projectRepository, ILogger<CreateTaskCommandHandler> logger)
        {
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _logger = logger;
        }

        public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
            // Check if project exists
            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                throw new NotFoundException();
            }

            // RN-01: Cannot create task under archived project
            if (await _taskRepository.IsProjectArchivedAsync(request.ProjectId))
            {
                _logger.LogWarning(
                    "Business rule violation. Operation: {Operation}, ResourceType: {ResourceType}, ProjectId: {ProjectId}, FailureCategory: {FailureCategory}",
                    "CreateTask",
                    "Project",
                    request.ProjectId,
                    "ArchivedProject");
                throw new DomainException("Cannot create a task under an archived project.");
            }

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Status = VALUM.Domain.Enums.TaskStatus.Todo,
                DueDate = request.DueDate,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = null,
                IsDeleted = false
            };

            await _taskRepository.AddAsync(task);

            _logger.LogInformation(
                "Task created. Operation: {Operation}, TaskId: {TaskId}, ProjectId: {ProjectId}",
                "CreateTask",
                task.Id,
                task.ProjectId);

            return new TaskDto
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                Status = task.Status,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt
            };
        }
    }
}
