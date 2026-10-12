using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VALUM.Application.Exceptions;
using VALUM.Application.Projects;
using VALUM.Application.Tasks;
using VALUM.Domain.Exceptions;
using VALUM.Domain.Enums;

namespace VALUM.Application.Projects.Commands
{
    public class ArchiveProjectCommandHandler : IRequestHandler<ArchiveProjectCommand>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<ArchiveProjectCommandHandler> _logger;

        public ArchiveProjectCommandHandler(IProjectRepository projectRepository, ITaskRepository taskRepository, ILogger<ArchiveProjectCommandHandler> logger)
        {
            _projectRepository = projectRepository;
            _taskRepository = taskRepository;
            _logger = logger;
        }

        public async Task Handle(ArchiveProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _projectRepository.GetByIdAsync(request.Id);
            
            if (project == null)
            {
                throw new NotFoundException();
            }

            // Idempotent: if already archived, return successfully without changes
            if (project.Status == ProjectStatus.Archived)
            {
                return;
            }

            // RN-02: Cannot archive project with nondeleted InProgress tasks
            if (await _taskRepository.HasActiveInProgressTasksAsync(request.Id))
            {
                _logger.LogWarning(
                    "Business rule violation. Operation: {Operation}, ResourceType: {ResourceType}, ProjectId: {ProjectId}, FailureCategory: {FailureCategory}",
                    "ArchiveProject",
                    "Project",
                    request.Id,
                    "TasksInProgress");
                throw new DomainException("Cannot archive a project with tasks in progress.");
            }

            // Archive the project
            project.Status = ProjectStatus.Archived;
            await _projectRepository.UpdateAsync(project);

            _logger.LogInformation(
                "Project archived. Operation: {Operation}, ProjectId: {ProjectId}",
                "ArchiveProject",
                project.Id);
        }
    }
}
