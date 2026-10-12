using FluentValidation;
using VALUM.Application.Tasks.Queries;

namespace VALUM.Application.Tasks.Queries
{
    public class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
    {
        public GetTasksQueryValidator()
        {
            RuleFor(x => x.ProjectId).NotEqual(System.Guid.Empty).WithMessage("ProjectId must be a non-empty GUID");
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(50);
        }
    }
}
