using System;
using FluentValidation;
using VALUM.Domain.Enums;

namespace VALUM.Application.Tasks.Commands
{
    public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEqual(Guid.Empty).WithMessage("ProjectId must be a non-empty GUID");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Title must not be whitespace")
                .MaximumLength(100).WithMessage("Title must be at most 100 characters");

            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Priority must be a valid TaskPriority value (0=Low, 1=Medium, 2=High)");
        }
    }
}
