using MediatR;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Projects.DTOs;
using VALUM.Domain.Enums;

namespace VALUM.Application.Projects.Queries
{
    public class GetProjectsQuery : IRequest<PagedResult<ProjectDto>>
    {
        public ProjectStatus? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
