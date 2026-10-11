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
    public class DatabaseInitializerTests
    {
        [Fact]
        public async Task ApplyMigrations_CreatesSqliteDatabaseAndSchema_WhenMissing()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"valum_test_{Guid.NewGuid():N}.db");
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = tempFile, Pooling = false };
                var connectionString = csb.ToString();

                var services = new ServiceCollection();
                services.AddInfrastructure(connectionString);
                await using var provider = services.BuildServiceProvider();

                // Act - call the future initializer
                await DatabaseInitializer.ApplyMigrationsAsync(provider, CancellationToken.None);

                // Assert - file and schema
                Assert.True(File.Exists(tempFile));

                var connectionStringCheck = new SqliteConnectionStringBuilder { DataSource = tempFile, Pooling = false }.ToString();
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connectionStringCheck)
                    .Options;

                using var ctx = new AppDbContext(options);
                var conn = ctx.Database.GetDbConnection();
                await conn.OpenAsync();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Projects';";
                    var existsObj = cmd.ExecuteScalar();
                    var existsProjects = existsObj != null;

                    cmd.CommandText = "SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE MigrationId LIKE '%InitialCreate%';";
                    var countObj = cmd.ExecuteScalar();
                    var count = Convert.ToInt32(countObj ?? 0);

                    Assert.True(existsProjects, "Projects table should exist after migrations");
                    Assert.True(count > 0, "__EFMigrationsHistory should contain InitialCreate");
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ApplyMigrations_IsIdempotent_AndPreservesExistingData()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"valum_test_{Guid.NewGuid():N}.db");
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = tempFile, Pooling = false };
                var connectionString = csb.ToString();

                var services = new ServiceCollection();
                services.AddInfrastructure(connectionString);
                await using var provider = services.BuildServiceProvider();

                // First apply
                await DatabaseInitializer.ApplyMigrationsAsync(provider, CancellationToken.None);

                // Insert a project
                using (var scope = provider.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var project = new Project { Name = "TDD Project", CreatedAt = DateTime.UtcNow, OwnerId = Guid.NewGuid() };
                    ctx.Projects.Add(project);
                    await ctx.SaveChangesAsync();
                }

                // Apply again
                await DatabaseInitializer.ApplyMigrationsAsync(provider, CancellationToken.None);

                // Verify project still exists and migration history contains exactly one InitialCreate entry
                using (var scope = provider.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var projects = ctx.Projects.ToList();
                    Assert.Single(projects);

                    var conn = ctx.Database.GetDbConnection();
                    await conn.OpenAsync();
                    try
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE MigrationId LIKE '%InitialCreate%';";
                        var countObj = cmd.ExecuteScalar();
                        var historyCount = Convert.ToInt32(countObj ?? 0);
                        Assert.Equal(1, historyCount);
                    }
                    finally
                    {
                        await conn.CloseAsync();
                    }
                }
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
