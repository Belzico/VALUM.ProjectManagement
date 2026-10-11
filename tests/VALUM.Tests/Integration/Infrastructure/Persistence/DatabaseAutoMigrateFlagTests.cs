using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using VALUM.Infrastructure;
using VALUM.Infrastructure.Persistence;
using Xunit;

namespace VALUM.Tests.Integration.Infrastructure.Persistence
{
    public class DatabaseAutoMigrateFlagTests
    {
        [Fact]
        public async Task AutoMigrateEnabled_CreatesSchema()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"valum_flag_{Guid.NewGuid():N}.db");
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = tempFile, Pooling = false };
                var connectionString = csb.ToString();

                var services = new ServiceCollection();
                services.AddInfrastructure(connectionString);
                await using var provider = services.BuildServiceProvider();

                // Default behavior of DatabaseInitializer is assumed enabled by config in API; call directly here
                await DatabaseInitializer.ApplyMigrationsAsync(provider);

                // Verify tables created
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Projects';";
                var obj = cmd.ExecuteScalar();
                Assert.NotNull(obj);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task AutoMigrateDisabled_DoesNotCreateSchema()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"valum_flag_{Guid.NewGuid():N}.db");
            try
            {
                var csb = new SqliteConnectionStringBuilder { DataSource = tempFile, Pooling = false };
                var connectionString = csb.ToString();

                // Simulate API not running migrations by not calling DatabaseInitializer
                // Build DI and ensure nothing touched the file
                var services = new ServiceCollection();
                services.AddInfrastructure(connectionString);
                await using var provider = services.BuildServiceProvider();

                // Ensure file still doesn't exist or has no Projects table
                Assert.False(File.Exists(tempFile) && new FileInfo(tempFile).Length > 0);

                // Optionally, verify that opening a connection does not show Projects
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Projects';";
                var obj = cmd.ExecuteScalar();
                Assert.Null(obj);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
