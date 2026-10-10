using System;
using Xunit;

namespace VALUM.Tests.Unit.Domain.Entities
{
    public class ProjectTests
    {
        [Fact]
        public void NewProject_HasDefaults_AndCanStoreData()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var project = new VALUM.Domain.Entities.Project();

            var after = DateTime.UtcNow;

            // Assert defaults
            Assert.NotEqual(Guid.Empty, project.Id);
            Assert.Equal(VALUM.Domain.Enums.ProjectStatus.Active, project.Status);
            Assert.Equal(DateTimeKind.Utc, project.CreatedAt.Kind);
            Assert.InRange(project.CreatedAt, before.AddSeconds(-5), after.AddSeconds(5));

            // Optional fields can be null
            project.Description = null;
            Assert.Null(project.Description);

            // Can store required business data
            project.Name = "Test Project";
            var owner = Guid.NewGuid();
            project.OwnerId = owner;

            Assert.Equal("Test Project", project.Name);
            Assert.Equal(owner, project.OwnerId);
        }

        [Fact]
        public void ProjectStatusEnum_HasExpectedValuesInOrder()
        {
            var names = Enum.GetNames(typeof(VALUM.Domain.Enums.ProjectStatus));
            Assert.Equal(new[] { "Active", "Archived" }, names);
        }
    }
}
