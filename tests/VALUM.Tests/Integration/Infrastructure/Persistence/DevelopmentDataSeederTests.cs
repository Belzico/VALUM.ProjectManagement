using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VALUM.Infrastructure;
using VALUM.Infrastructure.Persistence;
using VALUM.Domain.Entities;
using Xunit;

namespace VALUM.Tests.Integration.Infrastructure.Persistence
{
    public class DevelopmentDataSeederTests
    {
        private static string NewTempDb()
        {
            var path = Path.Combine(Path.GetTempPath(), $"valum_seed_{Guid.NewGuid():N}.db");
            if (File.Exists(path)) File.Delete(path);
            return path;
        }

        [Fact]
        public async Task Seed_Creates5ProjectsAnd20Tasks_WhenEnabledAndDevelopment()
        {
            var db = NewTempDb();
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = db, Pooling = false };
                var connectionString = csb.ToString();

                var services = new ServiceCollection();
                services.AddInfrastructure(connectionString);
                await using var provider = services.BuildServiceProvider();

                // Ensure migrations applied
                await DatabaseInitializer.ApplyMigrationsAsync(provider);

                // Act - call future seeder
                await DevelopmentDataSeeder.SeedAsync(provider, enabled: true, isDevelopment: true, cancellationToken: CancellationToken.None);

                // Assert
                await using var scope = provider.CreateAsyncScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var projects = await ctx.Projects.ToListAsync();
                var tasks = await ctx.Tasks.ToListAsync();

                Assert.Equal(5, projects.Count);
                Assert.Equal(20, tasks.Count);

                // New focused assertions: ensure distribution of statuses and priorities and CompletedAt behavior
                Assert.Contains(projects, p => p.Status == VALUM.Domain.Enums.ProjectStatus.Active);
                Assert.Contains(projects, p => p.Status == VALUM.Domain.Enums.ProjectStatus.Archived);

                Assert.Contains(tasks, t => t.Status == VALUM.Domain.Enums.TaskStatus.Todo);
                Assert.Contains(tasks, t => t.Status == VALUM.Domain.Enums.TaskStatus.InProgress);
                Assert.Contains(tasks, t => t.Status == VALUM.Domain.Enums.TaskStatus.Done);

                Assert.Contains(tasks, t => t.Priority == VALUM.Domain.Enums.TaskPriority.Low);
                Assert.Contains(tasks, t => t.Priority == VALUM.Domain.Enums.TaskPriority.Medium);
                Assert.Contains(tasks, t => t.Priority == VALUM.Domain.Enums.TaskPriority.High);

                // Done tasks must have CompletedAt set
                Assert.All(tasks.Where(t => t.Status == VALUM.Domain.Enums.TaskStatus.Done), t => Assert.NotNull(t.CompletedAt));
                // Non-done tasks must have CompletedAt null
                Assert.All(tasks.Where(t => t.Status != VALUM.Domain.Enums.TaskStatus.Done), t => Assert.Null(t.CompletedAt));

                // basic integrity
                Assert.All(projects, p => {
                    Assert.NotEqual(Guid.Empty, p.Id);
                    Assert.False(string.IsNullOrWhiteSpace(p.Name));
                    Assert.Equal(DateTimeKind.Utc, p.CreatedAt.Kind);
                });

                Assert.All(tasks, t => {
                    Assert.NotEqual(Guid.Empty, t.Id);
                    Assert.False(string.IsNullOrWhiteSpace(t.Title));
                    Assert.Equal(DateTimeKind.Utc, t.CreatedAt.Kind);
                    Assert.NotEqual(Guid.Empty, t.ProjectId);
                });

                // FK validity
                var projectIds = projects.Select(p => p.Id).ToHashSet();
                Assert.All(tasks.Select(t => t.ProjectId), pid => Assert.Contains(pid, projectIds));
            }
            finally
            {
                // ensure provider and scopes disposed before deleting
                if (File.Exists(db)) File.Delete(db);
            }
        }

        [Fact]
        public async Task Seed_IsIdempotent_NoDuplicatesOnRepeatedRuns()
        {
            var db = NewTempDb();
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = db, Pooling = false };
                var connectionString = csb.ToString();

                var services = new ServiceCollection();
                services.AddInfrastructure(connectionString);
                await using var provider = services.BuildServiceProvider();

                await DatabaseInitializer.ApplyMigrationsAsync(provider);

                await DevelopmentDataSeeder.SeedAsync(provider, enabled: true, isDevelopment: true, cancellationToken: CancellationToken.None);
                await DevelopmentDataSeeder.SeedAsync(provider, enabled: true, isDevelopment: true, cancellationToken: CancellationToken.None);

                await using var scope = provider.CreateAsyncScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var projects = await ctx.Projects.ToListAsync();
                var tasks = await ctx.Tasks.ToListAsync();

                Assert.Equal(5, projects.Count);
                Assert.Equal(20, tasks.Count);
            }
            finally
            {
                if (File.Exists(db)) File.Delete(db);
            }
        }

        [Fact]
        public async Task Seed_DoesNothing_WhenEnabledFalse()
        {
            var db = NewTempDb();
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = db, Pooling = false };
                var connectionString = csb.ToString();

                var services = new ServiceCollection();
                services.AddInfrastructure(connectionString);
                await using var provider = services.BuildServiceProvider();

                await DatabaseInitializer.ApplyMigrationsAsync(provider);

                await DevelopmentDataSeeder.SeedAsync(provider, enabled: false, isDevelopment: true, cancellationToken: CancellationToken.None);

                await using var scope = provider.CreateAsyncScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                Assert.Empty(await ctx.Projects.ToListAsync());
                Assert.Empty(await ctx.Tasks.ToListAsync());
            }
            finally
            {
                if (File.Exists(db)) File.Delete(db);
            }
        }

        [Fact]
        public async Task Seed_DoesNothing_WhenNotDevelopment()
        {
            var db = NewTempDb();
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = db, Pooling = false };
                var connectionString = csb.ToString();

                var services = new ServiceCollection();
                services.AddInfrastructure(connectionString);
                await using var provider = services.BuildServiceProvider();

                await DatabaseInitializer.ApplyMigrationsAsync(provider);

                await DevelopmentDataSeeder.SeedAsync(provider, enabled: true, isDevelopment: false, cancellationToken: CancellationToken.None);

                await using var scope = provider.CreateAsyncScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                Assert.Empty(await ctx.Projects.ToListAsync());
                Assert.Empty(await ctx.Tasks.ToListAsync());
            }
            finally
            {
                if (File.Exists(db)) File.Delete(db);
            }
        }

        [Fact]
        public async Task Seed_DoesNothing_WhenExistingProjectData()
        {
            var db = NewTempDb();
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = db, Pooling = false };
                var connectionString = csb.ToString();

                var services = new ServiceCollection();
                services.AddInfrastructure(connectionString);
                await using var provider = services.BuildServiceProvider();

                await DatabaseInitializer.ApplyMigrationsAsync(provider);

                // insert existing project
                await using (var scope = provider.CreateAsyncScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    ctx.Projects.Add(new Project { Name = "Existing", OwnerId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });
                    await ctx.SaveChangesAsync();
                }

                await DevelopmentDataSeeder.SeedAsync(provider, enabled: true, isDevelopment: true, cancellationToken: CancellationToken.None);

                await using var scope2 = provider.CreateAsyncScope();
                var ctx2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

                Assert.Single(await ctx2.Projects.ToListAsync());
                Assert.Empty(await ctx2.Tasks.ToListAsync());
            }
            finally
            {
                if (File.Exists(db)) File.Delete(db);
            }
        }

        [Fact]
        public async Task Seed_DoesNothing_WhenExistingTaskData()
        {
            var db = NewTempDb();
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = db, Pooling = false };
                var connectionString = csb.ToString();

                var services = new ServiceCollection();
                services.AddInfrastructure(connectionString);
                await using var provider = services.BuildServiceProvider();

                await DatabaseInitializer.ApplyMigrationsAsync(provider);

                // create a project and a task
                await using (var scope = provider.CreateAsyncScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var project = new Project { Name = "P", OwnerId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
                    ctx.Projects.Add(project);
                    await ctx.SaveChangesAsync();

                    var task = new TaskItem { Title = "T", ProjectId = project.Id, CreatedAt = DateTime.UtcNow, Priority = Domain.Enums.TaskPriority.Medium, Status = Domain.Enums.TaskStatus.Todo };
                    ctx.Tasks.Add(task);
                    await ctx.SaveChangesAsync();
                }

                await DevelopmentDataSeeder.SeedAsync(provider, enabled: true, isDevelopment: true, cancellationToken: CancellationToken.None);

                await using var scope2 = provider.CreateAsyncScope();
                var ctx2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

                Assert.Single(await ctx2.Projects.ToListAsync());
                Assert.Single(await ctx2.Tasks.ToListAsync());
            }
            finally
            {
                if (File.Exists(db)) File.Delete(db);
            }
        }

        [Fact]
        public async Task Seed_DoesNothing_WhenSoftDeletedTaskExists()
        {
            var db = NewTempDb();
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = db, Pooling = false };
                var connectionString = csb.ToString();

                var services = new ServiceCollection();
                services.AddInfrastructure(connectionString);
                await using var provider = services.BuildServiceProvider();

                await DatabaseInitializer.ApplyMigrationsAsync(provider);

                // create a project and a soft-deleted task
                await using (var scope = provider.CreateAsyncScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var project = new Project { Name = "P", OwnerId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
                    ctx.Projects.Add(project);
                    await ctx.SaveChangesAsync();

                    var task = new TaskItem { Title = "T", ProjectId = project.Id, CreatedAt = DateTime.UtcNow, Priority = Domain.Enums.TaskPriority.Medium, Status = Domain.Enums.TaskStatus.Todo, IsDeleted = true };
                    ctx.Tasks.Add(task);
                    await ctx.SaveChangesAsync();
                }

                await DevelopmentDataSeeder.SeedAsync(provider, enabled: true, isDevelopment: true, cancellationToken: CancellationToken.None);

                await using var scope2 = provider.CreateAsyncScope();
                var ctx2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

                Assert.Single(await ctx2.Projects.ToListAsync());
                Assert.Single(await ctx2.Tasks.ToListAsync());
            }
            finally
            {
                if (File.Exists(db)) File.Delete(db);
            }
        }
    }
}
