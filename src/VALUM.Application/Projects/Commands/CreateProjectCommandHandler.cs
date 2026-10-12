using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VALUM.Application.Projects.DTOs;
using VALUM.Domain.Entities;

namespace VALUM.Application.Projects.Commands
{
    public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
    {
        private readonly IProjectRepository _repository;
        private readonly ILogger<CreateProjectCommandHandler> _logger;

        public CreateProjectCommandHandler(IProjectRepository repository, ILogger<CreateProjectCommandHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Status = VALUM.Domain.Enums.ProjectStatus.Active,
                CreatedAt = DateTime.UtcNow,
                OwnerId = request.OwnerId
            };

            await _repository.AddAsync(project);

            _logger.LogInformation(
                "Project created. Operation: {Operation}, ProjectId: {ProjectId}",
                "CreateProject",
                project.Id);

            return new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status,
                CreatedAt = project.CreatedAt,
                OwnerId = project.OwnerId
            };
        }
    }
}
