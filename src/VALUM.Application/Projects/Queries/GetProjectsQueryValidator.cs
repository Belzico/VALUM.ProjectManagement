using FluentValidation;
using VALUM.Application.Projects.Queries;

namespace VALUM.Application.Projects.Queries
{
    public class GetProjectsQueryValidator : AbstractValidator<GetProjectsQuery>
    {
        public GetProjectsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(50);
        }
    }
}
