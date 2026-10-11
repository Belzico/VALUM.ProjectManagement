using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VALUM.Domain.Entities;
using Xunit;

namespace VALUM.Tests.Integration.Infrastructure.Persistence
{
    // These tests describe the expected behavior and API of
    // VALUM.Infrastructure.Persistence.AppDbContext and related EF Core mapping.
    // The production AppDbContext is not implemented yet (TDD: RED).
    public class AppDbContextTests
    {
        private static DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext> GetOptionsBuilder(SqliteConnection connection)
        {
            var builder = new DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            builder.UseSqlite(connection);
            return builder;
        }

        [Fact]
        public async Task CanSaveAndRetrieveProject()
        {
            // Arrange: open an in-memory SQLite connection and keep it open for the lifetime of the test
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var optionsBuilder = GetOptionsBuilder(connection);

            // Act: create schema and save a project
            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                await context.Database.EnsureCreatedAsync();

                var project = new Project
                {
                    Name = "Integration Project",
                    OwnerId = Guid.NewGuid()
                };

                context.Projects.Add(project);
                await context.SaveChangesAsync();
            }

            // Assert: use a fresh context instance sharing the same open connection
            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                var loaded = await context.Projects.FirstOrDefaultAsync();
                Assert.NotNull(loaded);
                Assert.Equal("Integration Project", loaded.Name);
            }
        }

        [Fact]
        public async Task CanSaveAndRetrieveTask()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var optionsBuilder = GetOptionsBuilder(connection);

            Guid taskId;
            Guid projectId;

            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                await context.Database.EnsureCreatedAsync();

                var project = new Project { Name = "P1", OwnerId = Guid.NewGuid() };
                context.Projects.Add(project);
                await context.SaveChangesAsync();

                var task = new TaskItem { Title = "T1", ProjectId = project.Id, Priority = VALUM.Domain.Enums.TaskPriority.Low };
                context.Tasks.Add(task);
                await context.SaveChangesAsync();

                taskId = task.Id;
                projectId = project.Id;
            }

            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                var loaded = await context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
                Assert.NotNull(loaded);
                Assert.Equal(projectId, loaded.ProjectId);
            }
        }

        [Fact]
        public async Task TaskRequiresExistingProject()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            // Enable SQLite foreign keys pragmas on the connection so FK constraints are enforced
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = ON;";
                cmd.ExecuteNonQuery();
            }

            var optionsBuilder = GetOptionsBuilder(connection);

            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                await context.Database.EnsureCreatedAsync();
            }

            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                // Attempt to save a TaskItem referencing a non-existent Project
                var task = new TaskItem { Title = "Orphan", ProjectId = Guid.NewGuid(), Priority = VALUM.Domain.Enums.TaskPriority.Medium };
                context.Tasks.Add(task);

                await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(async () =>
                {
                    await context.SaveChangesAsync();
                });
            }
        }

        [Fact]
        public async Task PersistsRequiredFieldsAndEnums()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var optionsBuilder = GetOptionsBuilder(connection);

            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                await context.Database.EnsureCreatedAsync();

                var project = new Project { Name = "ProjectX", OwnerId = Guid.NewGuid(), Status = VALUM.Domain.Enums.ProjectStatus.Active };
                context.Projects.Add(project);

                var task = new TaskItem { Title = "TaskX", ProjectId = project.Id, Priority = VALUM.Domain.Enums.TaskPriority.High, Status = VALUM.Domain.Enums.TaskStatus.InProgress };
                context.Tasks.Add(task);

                await context.SaveChangesAsync();
            }

            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                var p = await context.Projects.FirstOrDefaultAsync();
                var t = await context.Tasks.FirstOrDefaultAsync();

                Assert.NotNull(p);
                Assert.NotNull(t);
                Assert.Equal("ProjectX", p.Name);
                Assert.Equal(VALUM.Domain.Enums.ProjectStatus.Active, p.Status);

                Assert.Equal("TaskX", t.Title);
                Assert.Equal(VALUM.Domain.Enums.TaskPriority.High, t.Priority);
                Assert.Equal(VALUM.Domain.Enums.TaskStatus.InProgress, t.Status);
            }
        }

        [Fact]
        public async Task UtcDatesSurviveSqliteRoundTrip()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var optionsBuilder = GetOptionsBuilder(connection);

            var created = DateTime.UtcNow;
            var due = DateTime.UtcNow.AddDays(1);
            var completed = DateTime.UtcNow.AddDays(2);

            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                await context.Database.EnsureCreatedAsync();

                var project = new Project { Name = "Pdt", OwnerId = Guid.NewGuid(), CreatedAt = created };
                context.Projects.Add(project);

                var task = new TaskItem
                {
                    Title = "Tdt",
                    ProjectId = project.Id,
                    CreatedAt = created,
                    DueDate = due,
                    CompletedAt = completed
                };
                context.Tasks.Add(task);

                await context.SaveChangesAsync();
            }

            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                var p = await context.Projects.FirstOrDefaultAsync();
                var t = await context.Tasks.FirstOrDefaultAsync();

                Assert.NotNull(p);
                Assert.NotNull(t);

                Assert.Equal(DateTimeKind.Utc, p.CreatedAt.Kind);
                Assert.Equal(DateTimeKind.Utc, t.CreatedAt.Kind);

                Assert.Equal(created, p.CreatedAt);
                Assert.Equal(created, t.CreatedAt);

                Assert.NotNull(t.DueDate);
                Assert.Equal(DateTimeKind.Utc, t.DueDate.Value.Kind);
                Assert.Equal(due, t.DueDate.Value);

                Assert.NotNull(t.CompletedAt);
                Assert.Equal(DateTimeKind.Utc, t.CompletedAt.Value.Kind);
                Assert.Equal(completed, t.CompletedAt.Value);
            }

        }

        // TDD: Tests that persistence layer rejects non-UTC DateTime values.
        // These tests are intentionally written before production guards exist (RED phase).
        [Theory]
        [InlineData("ProjectCreatedAt", DateTimeKind.Local)]
        [InlineData("ProjectCreatedAt", DateTimeKind.Unspecified)]
        [InlineData("TaskCreatedAt", DateTimeKind.Local)]
        [InlineData("TaskCreatedAt", DateTimeKind.Unspecified)]
        [InlineData("TaskDueDate", DateTimeKind.Local)]
        [InlineData("TaskDueDate", DateTimeKind.Unspecified)]
        [InlineData("TaskCompletedAt", DateTimeKind.Local)]
        [InlineData("TaskCompletedAt", DateTimeKind.Unspecified)]
        public async Task SaveChanges_RejectsNonUtcDateTimes(string property, DateTimeKind kind)
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var optionsBuilder = GetOptionsBuilder(connection);

            // Prepare a known UTC baseline
            var utcNow = DateTime.UtcNow;

            // build an invalid DateTime with requested kind
            DateTime MakeInvalid(DateTime src, DateTimeKind k)
            {
                return DateTime.SpecifyKind(src, k);
            }

            // create schema
            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                await context.Database.EnsureCreatedAsync();
            }

            // Attempt to add entities with a single invalid date field
            await using (var context = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilder.Options))
            {
                var project = new Project
                {
                    Name = "P",
                    OwnerId = Guid.NewGuid(),
                    CreatedAt = utcNow
                };

                var task = new TaskItem
                {
                    Title = "T",
                    ProjectId = project.Id,
                    CreatedAt = utcNow,
                    DueDate = null,
                    CompletedAt = null,
                    Priority = VALUM.Domain.Enums.TaskPriority.Low
                };

                // inject the invalid value into the targeted property
                var invalid = MakeInvalid(utcNow, kind);

                switch (property)
                {
                    case "ProjectCreatedAt":
                        project.CreatedAt = invalid;
                        break;
                    case "TaskCreatedAt":
                        task.CreatedAt = invalid;
                        break;
                    case "TaskDueDate":
                        task.DueDate = invalid;
                        break;
                    case "TaskCompletedAt":
                        task.CompletedAt = invalid;
                        break;
                    default:
                        throw new InvalidOperationException("Unknown property");
                }

                context.Projects.Add(project);
                context.Tasks.Add(task);

                var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await context.SaveChangesAsync());

                Assert.Contains(property, ex.Message);
                Assert.Contains("UTC", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
