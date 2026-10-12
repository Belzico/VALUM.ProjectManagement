using System;
using MediatR;
using VALUM.Application.Projects.DTOs;

namespace VALUM.Application.Projects.Queries
{
    public class GetProjectByIdQuery : IRequest<GetProjectByIdResult>
    {
        public Guid Id { get; set; }
    }

    public class GetProjectByIdResult
    {
        public ProjectDto Project { get; set; } = null!;
        public ProjectTaskSummary Summary { get; set; } = null!;
    }
}
