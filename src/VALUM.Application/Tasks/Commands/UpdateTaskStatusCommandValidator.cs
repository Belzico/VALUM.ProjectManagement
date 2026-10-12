using System;
using FluentValidation;
using VALUM.Application.Tasks.Commands;
using VALUM.Domain.Enums;

namespace VALUM.Application.Tasks.Commands
{
    public class UpdateTaskStatusCommandValidator : AbstractValidator<UpdateTaskStatusCommand>
    {
        public UpdateTaskStatusCommandValidator()
        {
            RuleFor(x => x.Id).NotEqual(System.Guid.Empty).WithMessage("Id must be a non-empty GUID");
            RuleFor(x => x.Status).IsInEnum().WithMessage("Status must be a valid TaskStatus value (0=Todo, 1=InProgress, 2=Done)");
        }
    }
}
