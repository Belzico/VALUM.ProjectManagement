using System;
using Xunit;

namespace VALUM.Tests.Unit.Domain.Entities
{
    public class TaskItemTests
    {
        [Fact]
        public void NewTaskItem_HasDefaults_AndCanStoreData()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var task = new VALUM.Domain.Entities.TaskItem();

            var after = DateTime.UtcNow;

            // Assert defaults
            Assert.NotEqual(Guid.Empty, task.Id);
            Assert.Equal(VALUM.Domain.Enums.TaskStatus.Todo, task.Status);
            Assert.False(task.IsDeleted);
            Assert.Null(task.CompletedAt);
            Assert.Equal(DateTimeKind.Utc, task.CreatedAt.Kind);
            Assert.InRange(task.CreatedAt, before.AddSeconds(-5), after.AddSeconds(5));

            // Optional fields can be null
            task.Description = null;
            task.DueDate = null;
            Assert.Null(task.Description);
            Assert.Null(task.DueDate);

            // Can store required business data
            task.Title = "Test Task";
            task.ProjectId = Guid.NewGuid();
            task.Priority = VALUM.Domain.Enums.TaskPriority.Medium;

            Assert.Equal("Test Task", task.Title);
            Assert.Equal(VALUM.Domain.Enums.TaskPriority.Medium, task.Priority);
            Assert.NotEqual(Guid.Empty, task.ProjectId);
        }

        [Fact]
        public void TaskEnums_HaveExpectedValuesInOrder()
        {
            var statusNames = Enum.GetNames(typeof(VALUM.Domain.Enums.TaskStatus));
            Assert.Equal(new[] { "Todo", "InProgress", "Done" }, statusNames);

            var priorityNames = Enum.GetNames(typeof(VALUM.Domain.Enums.TaskPriority));
            Assert.Equal(new[] { "Low", "Medium", "High" }, priorityNames);
        }
    }
}
