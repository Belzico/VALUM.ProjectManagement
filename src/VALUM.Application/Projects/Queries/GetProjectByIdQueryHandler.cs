using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VALUM.Application.Projects.DTOs;
using VALUM.Application.Projects;
using VALUM.Application.Exceptions;

namespace VALUM.Application.Projects.Queries
{
    public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, GetProjectByIdResult>
    {
        private readonly IProjectRepository _repository;

        public GetProjectByIdQueryHandler(IProjectRepository repository)
        {
            _repository = repository;
        }

        public async Task<GetProjectByIdResult> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
        {
            var proj = await _repository.GetByIdAsync(request.Id);
            if (proj == null)
            {
                throw new NotFoundException();
            }

            var summary = await _repository.GetTaskSummaryAsync(request.Id);

            return new GetProjectByIdResult
            {
                Project = new ProjectDto
                {
                    Id = proj.Id,
                    Name = proj.Name,
                    Description = proj.Description,
                    Status = proj.Status,
                    CreatedAt = proj.CreatedAt,
                    OwnerId = proj.OwnerId
                },
                Summary = summary ?? new ProjectTaskSummary { Total = 0, Completed = 0, InProgress = 0, Pending = 0 }
            };
        }
    }
}
