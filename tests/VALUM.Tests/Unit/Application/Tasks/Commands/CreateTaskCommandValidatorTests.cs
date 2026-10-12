using FluentValidation.TestHelper;
using VALUM.Application.Tasks.Commands;
using VALUM.Domain.Enums;
using Xunit;

namespace VALUM.Tests.Unit.Application.Tasks.Commands
{
    public class CreateTaskCommandValidatorTests
    {
        private readonly CreateTaskCommandValidator _validator;

        public CreateTaskCommandValidatorTests()
        {
            _validator = new CreateTaskCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveError()
        {
            var command = new CreateTaskCommand
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Task",
                Priority = TaskPriority.Medium
            };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyProjectId_ShouldHaveError()
        {
            var command = new CreateTaskCommand
            {
                ProjectId = Guid.Empty,
                Title = "Test Task",
                Priority = TaskPriority.Medium
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.ProjectId);
        }

        [Fact]
        public void Validate_EmptyTitle_ShouldHaveError()
        {
            var command = new CreateTaskCommand
            {
                ProjectId = Guid.NewGuid(),
                Title = "",
                Priority = TaskPriority.Medium
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_WhitespaceTitle_ShouldHaveError()
        {
            var command = new CreateTaskCommand
            {
                ProjectId = Guid.NewGuid(),
                Title = "   ",
                Priority = TaskPriority.Medium
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_TitleOver100Characters_ShouldHaveError()
        {
            var command = new CreateTaskCommand
            {
                ProjectId = Guid.NewGuid(),
                Title = new string('A', 101),
                Priority = TaskPriority.Medium
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_TitleExactly100Characters_ShouldNotHaveError()
        {
            var command = new CreateTaskCommand
            {
                ProjectId = Guid.NewGuid(),
                Title = new string('A', 100),
                Priority = TaskPriority.Medium
            };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_InvalidPriority_ShouldHaveError()
        {
            var command = new CreateTaskCommand
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Task",
                Priority = (TaskPriority)99
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Priority);
        }

        [Fact]
        public void Validate_AllValidPriorities_ShouldNotHaveError()
        {
            foreach (var priority in new[] { TaskPriority.Low, TaskPriority.Medium, TaskPriority.High })
            {
                var command = new CreateTaskCommand
                {
                    ProjectId = Guid.NewGuid(),
                    Title = "Test Task",
                    Priority = priority
                };
                var result = _validator.TestValidate(command);
                result.ShouldNotHaveAnyValidationErrors();
            }
        }
    }
}
