using System;
using FluentValidation;

namespace VALUM.Application.Projects.Commands
{
    public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
    {
        public CreateProjectCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Name must not be whitespace")
                .MaximumLength(80).WithMessage("Name must be at most 80 characters");

            RuleFor(x => x.OwnerId)
                .NotEqual(Guid.Empty).WithMessage("OwnerId must be a non-empty GUID");
        }
    }
}
