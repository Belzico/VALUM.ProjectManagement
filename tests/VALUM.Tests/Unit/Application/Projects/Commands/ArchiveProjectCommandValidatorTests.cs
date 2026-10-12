using FluentValidation.TestHelper;
using VALUM.Application.Projects.Commands;
using Xunit;

namespace VALUM.Tests.Unit.Application.Projects.Commands
{
    public class ArchiveProjectCommandValidatorTests
    {
        private readonly ArchiveProjectCommandValidator _validator;

        public ArchiveProjectCommandValidatorTests()
        {
            _validator = new ArchiveProjectCommandValidator();
        }

        [Fact]
        public void Validate_ValidId_ShouldNotHaveError()
        {
            var command = new ArchiveProjectCommand { Id = Guid.NewGuid() };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyId_ShouldHaveError()
        {
            var command = new ArchiveProjectCommand { Id = Guid.Empty };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Id);
        }
    }
}
