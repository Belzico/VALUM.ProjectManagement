using MediatR;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Tasks.DTOs;
using VALUM.Domain.Enums;

namespace VALUM.Application.Tasks.Queries
{
    public class GetTasksByCursorQuery : IRequest<CursorPagedResult<TaskDto>>
    {
        public Guid ProjectId { get; set; }
        public string? Cursor { get; set; }
        public int PageSize { get; set; } = 10;
        public VALUM.Domain.Enums.TaskStatus? Status { get; set; }
        public TaskPriority? Priority { get; set; }
    }
}
