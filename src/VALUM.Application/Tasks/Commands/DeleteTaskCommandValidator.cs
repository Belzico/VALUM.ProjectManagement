using System;
using FluentValidation;
using VALUM.Application.Tasks.Commands;

namespace VALUM.Application.Tasks.Commands
{
    public class DeleteTaskCommandValidator : AbstractValidator<DeleteTaskCommand>
    {
        public DeleteTaskCommandValidator()
        {
            RuleFor(x => x.Id).NotEqual(System.Guid.Empty).WithMessage("Id must be a non-empty GUID");
        }
    }
}
