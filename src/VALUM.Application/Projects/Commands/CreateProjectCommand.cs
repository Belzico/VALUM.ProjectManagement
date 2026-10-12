using System;
using MediatR;
using VALUM.Application.Projects.DTOs;

namespace VALUM.Application.Projects.Commands
{
    public class CreateProjectCommand : IRequest<ProjectDto>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
    }
}
