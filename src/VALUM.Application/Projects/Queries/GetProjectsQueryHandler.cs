using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Projects.DTOs;
// no EF Core references here; repository provides read methods
using VALUM.Domain.Enums;
using VALUM.Application.Projects;
using System.Collections.Generic;

namespace VALUM.Application.Projects.Queries
{
    public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, PagedResult<ProjectDto>>
    {
        private readonly IProjectRepository _repository;

        public GetProjectsQueryHandler(IProjectRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResult<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
        {
            var skip = (request.Page - 1) * request.PageSize;
            var total = await _repository.CountAsync(request.Status);
            var page = await _repository.GetPageAsync(request.Status, skip, request.PageSize);

            var items = page.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                OwnerId = p.OwnerId
            }).ToList();

            return new PagedResult<ProjectDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = total
            };
        }
    }
}
