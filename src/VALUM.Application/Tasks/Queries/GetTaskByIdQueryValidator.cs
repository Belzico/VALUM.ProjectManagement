using System;
using FluentValidation;
using VALUM.Application.Tasks.Queries;

namespace VALUM.Application.Tasks.Queries
{
    public class GetTaskByIdQueryValidator : AbstractValidator<GetTaskByIdQuery>
    {
        public GetTaskByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEqual(System.Guid.Empty).WithMessage("Id must be a non-empty GUID");
        }
    }
}
