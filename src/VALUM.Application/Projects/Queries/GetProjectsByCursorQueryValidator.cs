using FluentValidation;
using VALUM.Domain.Enums;

namespace VALUM.Application.Projects.Queries
{
    public class GetProjectsByCursorQueryValidator : AbstractValidator<GetProjectsByCursorQuery>
    {
        public GetProjectsByCursorQueryValidator()
        {
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be at least 1")
                .LessThanOrEqualTo(50).WithMessage("PageSize cannot exceed 50");
        }
    }
}
