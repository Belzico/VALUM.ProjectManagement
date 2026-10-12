using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Exceptions;
using VALUM.Application.Projects;
using VALUM.Application.Tasks.DTOs;

namespace VALUM.Application.Tasks.Queries
{
    public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, PagedResult<TaskDto>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IProjectRepository _projectRepository;

        public GetTasksQueryHandler(ITaskRepository taskRepository, IProjectRepository projectRepository)
        {
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
        }

        public async Task<PagedResult<TaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
        {
            // Check if project exists
            var projectExists = await _projectRepository.ExistsAsync(request.ProjectId);
            if (!projectExists)
            {
                throw new NotFoundException();
            }

            var skip = (request.Page - 1) * request.PageSize;
            var total = await _taskRepository.CountAsync(request.ProjectId, request.Status, request.Priority);
            var page = await _taskRepository.GetPageAsync(request.ProjectId, request.Status, request.Priority, skip, request.PageSize);

            var items = page.Select(t => new TaskDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority,
                Status = t.Status,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt
            }).ToList();

            return new PagedResult<TaskDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = total
            };
        }
    }
}
