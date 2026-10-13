using HotChocolate.Types;
using VALUM.Application.Projects.DTOs;
using VALUM.Domain.Enums;

namespace VALUM.Api.GraphQL.Types
{
    public class ProjectType : ObjectType<ProjectDto>
    {
        protected override void Configure(IObjectTypeDescriptor<ProjectDto> descriptor)
        {
            descriptor.Name("Project");
            descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
            descriptor.Field(p => p.Name).Type<NonNullType<StringType>>();
            descriptor.Field(p => p.Description).Type<StringType>();
            descriptor.Field(p => p.Status).Type<NonNullType<EnumType<ProjectStatus>>>();
            descriptor.Field(p => p.CreatedAt).Type<NonNullType<DateTimeType>>();
            descriptor.Field(p => p.OwnerId).Type<NonNullType<IdType>>();
        }
    }
}
