using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using MediatR;
using VALUM.Application.Projects.Queries;
using VALUM.Application.Projects.DTOs;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Exceptions;
using VALUM.Domain.Enums;

namespace VALUM.Api.GraphQL
{
    public class Query
    {
        public async Task<PagedResult<ProjectDto>> Projects(
            [Service] IMediator mediator,
            int page = 1,
            int pageSize = 10,
            ProjectStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            if (page < 1)
                throw new GraphQLException("Page must be at least 1");
            if (pageSize < 1)
                throw new GraphQLException("PageSize must be at least 1");
            if (pageSize > 50)
                throw new GraphQLException("PageSize cannot exceed 50");

            var query = new GetProjectsQuery
            {
                Page = page,
                PageSize = pageSize,
                Status = status
            };

            return await mediator.Send(query, cancellationToken);
        }

        public async Task<GetProjectByIdResult?> Project(
            [Service] IMediator mediator,
            System.Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetProjectByIdQuery { Id = id };
                return await mediator.Send(query, cancellationToken);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }
    }
}
