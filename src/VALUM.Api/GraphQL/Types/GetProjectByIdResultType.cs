using HotChocolate.Types;
using VALUM.Application.Projects.Queries;

namespace VALUM.Api.GraphQL.Types
{
    public class GetProjectByIdResultType : ObjectType<GetProjectByIdResult?>
    {
        protected override void Configure(IObjectTypeDescriptor<GetProjectByIdResult?> descriptor)
        {
            descriptor.Name("GetProjectByIdResult");
            descriptor.Field(r => r!.Project).Type<ProjectType>().Name("project");
            descriptor.Field(r => r!.Summary).Type<ProjectTaskSummaryType>().Name("summary");
        }
    }
}
