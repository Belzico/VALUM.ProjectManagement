using HotChocolate.Types;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Projects.DTOs;

namespace VALUM.Api.GraphQL.Types
{
    public class PagedResultOfProjectType : ObjectType<PagedResult<ProjectDto>>
    {
        protected override void Configure(IObjectTypeDescriptor<PagedResult<ProjectDto>> descriptor)
        {
            descriptor.Name("PagedResultOfProject");
            descriptor.Field(p => p.Items).Type<NonNullType<ListType<NonNullType<ProjectType>>>>();
            descriptor.Field(p => p.Page).Type<NonNullType<IntType>>();
            descriptor.Field(p => p.PageSize).Type<NonNullType<IntType>>();
            descriptor.Field(p => p.TotalCount).Type<NonNullType<IntType>>();
            descriptor.Field(p => p.TotalPages).Type<NonNullType<IntType>>();
            descriptor.Field(p => p.HasPrevious).Type<NonNullType<BooleanType>>();
            descriptor.Field(p => p.HasNext).Type<NonNullType<BooleanType>>();
        }
    }
}
