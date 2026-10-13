using HotChocolate.Types;
using VALUM.Application.Projects;

namespace VALUM.Api.GraphQL.Types
{
    public class ProjectTaskSummaryType : ObjectType<ProjectTaskSummary>
    {
        protected override void Configure(IObjectTypeDescriptor<ProjectTaskSummary> descriptor)
        {
            descriptor.Name("ProjectTaskSummary");
            descriptor.Field(s => s.Total).Type<NonNullType<IntType>>().Name("totalTasks");
            descriptor.Field(s => s.Completed).Type<NonNullType<IntType>>().Name("completedTasks");
            descriptor.Field(s => s.InProgress).Type<NonNullType<IntType>>().Name("inProgressTasks");
            descriptor.Field(s => s.Pending).Type<NonNullType<IntType>>().Name("pendingTasks");
        }
    }
}
