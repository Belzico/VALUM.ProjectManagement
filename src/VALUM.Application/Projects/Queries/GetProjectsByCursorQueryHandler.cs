using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Projects.DTOs;
using VALUM.Domain.Enums;

namespace VALUM.Application.Projects.Queries
{
    public class GetProjectsByCursorQueryHandler : IRequestHandler<GetProjectsByCursorQuery, CursorPagedResult<ProjectDto>>
    {
        private readonly IProjectRepository _repository;

        public GetProjectsByCursorQueryHandler(IProjectRepository repository)
        {
            _repository = repository;
        }

        public async Task<CursorPagedResult<ProjectDto>> Handle(GetProjectsByCursorQuery request, CancellationToken cancellationToken)
        {
            DateTime? cursorCreatedAt = null;
            Guid? cursorId = null;

            if (!string.IsNullOrEmpty(request.Cursor))
            {
                var token = CursorToken.Decode(request.Cursor);
                if (token == null || token.Scope != CursorScope.Projects)
                {
                    throw new ArgumentException("Invalid cursor");
                }

                if (token.StatusFilter != request.Status?.ToString() || token.PageSize != request.PageSize)
                {
                    throw new ArgumentException("Cursor does not match current filters or page size");
                }

                cursorCreatedAt = token.CreatedAt;
                cursorId = token.Id;
            }

            var take = request.PageSize + 1;
            var projects = await _repository.GetPageByCursorAsync(request.Status, cursorCreatedAt, cursorId, take);

            var hasNext = projects.Count > request.PageSize;
            var itemsToReturn = hasNext ? projects.Take(request.PageSize).ToList() : projects;

            var dtos = itemsToReturn.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                OwnerId = p.OwnerId
            }).ToList();

            string? nextCursor = null;
            if (hasNext && dtos.Count > 0)
            {
                var lastItem = dtos.Last();
                nextCursor = new CursorToken
                {
                    Scope = CursorScope.Projects,
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id,
                    PageSize = request.PageSize,
                    StatusFilter = request.Status?.ToString()
                }.Encode();
            }

            return new CursorPagedResult<ProjectDto>
            {
                Items = dtos,
                NextCursor = nextCursor,
                HasNext = hasNext
            };
        }
    }
}
