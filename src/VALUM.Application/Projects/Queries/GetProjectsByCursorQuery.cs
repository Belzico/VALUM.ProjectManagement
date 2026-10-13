using MediatR;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Projects.DTOs;
using VALUM.Domain.Enums;

namespace VALUM.Application.Projects.Queries
{
    public class GetProjectsByCursorQuery : IRequest<CursorPagedResult<ProjectDto>>
    {
        public string? Cursor { get; set; }
        public int PageSize { get; set; } = 10;
        public ProjectStatus? Status { get; set; }
    }
}
