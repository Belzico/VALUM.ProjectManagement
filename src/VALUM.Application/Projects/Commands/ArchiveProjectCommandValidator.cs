using FluentValidation;
using VALUM.Application.Projects.Commands;

namespace VALUM.Application.Projects.Commands
{
    public class ArchiveProjectCommandValidator : AbstractValidator<ArchiveProjectCommand>
    {
        public ArchiveProjectCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Project ID is required.");
        }
    }
}
