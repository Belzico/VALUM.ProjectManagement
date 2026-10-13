using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Tasks.DTOs;

namespace VALUM.Application.Tasks.Queries
{
    public class GetTasksByCursorQueryHandler : IRequestHandler<GetTasksByCursorQuery, CursorPagedResult<TaskDto>>
    {
        private readonly ITaskRepository _repository;

        public GetTasksByCursorQueryHandler(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<CursorPagedResult<TaskDto>> Handle(GetTasksByCursorQuery request, CancellationToken cancellationToken)
        {
            DateTime? cursorCreatedAt = null;
            Guid? cursorId = null;

            if (!string.IsNullOrEmpty(request.Cursor))
            {
                var token = CursorToken.Decode(request.Cursor);
                if (token == null || token.Scope != CursorScope.ProjectTasks)
                {
                    throw new ArgumentException("Invalid cursor");
                }

                if (token.ProjectId != request.ProjectId)
                {
                    throw new ArgumentException("Cursor does not match requested project");
                }

                if (token.StatusFilter != request.Status?.ToString() || 
                    token.PriorityFilter != request.Priority?.ToString() ||
                    token.PageSize != request.PageSize)
                {
                    throw new ArgumentException("Cursor does not match current filters or page size");
                }

                cursorCreatedAt = token.CreatedAt;
                cursorId = token.Id;
            }

            var take = request.PageSize + 1;
            var tasks = await _repository.GetPageByCursorAsync(request.ProjectId, request.Status, request.Priority, cursorCreatedAt, cursorId, take);

            var hasNext = tasks.Count > request.PageSize;
            var itemsToReturn = hasNext ? tasks.Take(request.PageSize).ToList() : tasks;

            var dtos = itemsToReturn.Select(t => new TaskDto
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

            string? nextCursor = null;
            if (hasNext && dtos.Count > 0)
            {
                var lastItem = dtos.Last();
                nextCursor = new CursorToken
                {
                    Scope = CursorScope.ProjectTasks,
                    ProjectId = request.ProjectId,
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id,
                    PageSize = request.PageSize,
                    StatusFilter = request.Status?.ToString(),
                    PriorityFilter = request.Priority?.ToString()
                }.Encode();
            }

            return new CursorPagedResult<TaskDto>
            {
                Items = dtos,
                NextCursor = nextCursor,
                HasNext = hasNext
            };
        }
    }
}
