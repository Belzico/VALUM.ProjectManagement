using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using VALUM.Infrastructure;
using VALUM.Infrastructure.Persistence;
using Xunit;

namespace VALUM.Tests.Integration.Infrastructure
{
    // TDD tests for the future VALUM.Infrastructure.DependencyInjection.AddInfrastructure
    // These tests are RED until AddInfrastructure is implemented.
    public class InfrastructureDependencyInjectionTests
    {
        [Fact]
        public void AddInfrastructure_Registers_AppDbContext_AsScoped()
        {
            var services = new ServiceCollection();

            // The extension AddInfrastructure will be implemented in VALUM.Infrastructure
            services.AddInfrastructure("DataSource=:memory:");

            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.NotNull(context);
        }

        [Fact]
        public void AppDbContext_UsesSqliteProvider_WithSuppliedConnectionString()
        {
            var services = new ServiceCollection();
            var conn = "DataSource=file:inf-dbi?mode=memory&cache=shared";

            services.AddInfrastructure(conn);

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Provider name for SQLite should contain 'Sqlite'
            Assert.Contains("Sqlite", context.Database.ProviderName, StringComparison.OrdinalIgnoreCase);

            // Connection string should contain the supplied data source
            var cs = context.Database.GetDbConnection().ConnectionString;
            Assert.Contains("DataSource=file:inf-dbi", cs, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AppDbContext_Is_Scoped_Behavior_Works()
        {
            var services = new ServiceCollection();
            services.AddInfrastructure("DataSource=:memory:");

            var provider = services.BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var a = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var b = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Same(a, b);
            }

            using (var s2 = provider.CreateScope())
            {
                var c = s2.ServiceProvider.GetRequiredService<AppDbContext>();
                // Different scope -> different instance
                Assert.NotSame(provider.GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider.GetRequiredService<AppDbContext>(), c);
            }
        }
    }
}
