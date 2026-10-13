using FluentValidation;

namespace VALUM.Application.Tasks.Queries
{
    public class GetTasksByCursorQueryValidator : AbstractValidator<GetTasksByCursorQuery>
    {
        public GetTasksByCursorQueryValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be at least 1")
                .LessThanOrEqualTo(50).WithMessage("PageSize cannot exceed 50");
        }
    }
}
