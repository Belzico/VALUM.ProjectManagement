using FluentValidation;
using VALUM.Application.Projects.Queries;
using System;

namespace VALUM.Application.Projects.Queries
{
    public class GetProjectByIdQueryValidator : AbstractValidator<GetProjectByIdQuery>
    {
        public GetProjectByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEqual(Guid.Empty);
        }
    }
}
