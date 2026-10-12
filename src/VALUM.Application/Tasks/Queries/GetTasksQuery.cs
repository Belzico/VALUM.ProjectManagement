using MediatR;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Tasks.DTOs;
using VALUM.Domain.Enums;

namespace VALUM.Application.Tasks.Queries
{
    public class GetTasksQuery : IRequest<PagedResult<TaskDto>>
    {
        public Guid ProjectId { get; set; }
        public VALUM.Domain.Enums.TaskStatus? Status { get; set; }
        public TaskPriority? Priority { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
