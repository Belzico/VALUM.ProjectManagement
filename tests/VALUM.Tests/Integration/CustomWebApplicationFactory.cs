using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using VALUM.Infrastructure.Persistence;
using System.Linq;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    public string DbPath { get; }

    public CustomWebApplicationFactory()
    {
        DbPath = Path.Combine(Path.GetTempPath(), "valum_test_" + Guid.NewGuid().ToString() + ".db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={DbPath}",
                ["SeedData:Enabled"] = "false",
                ["Database:AutoMigrate"] = "true"
            };

            config.AddInMemoryCollection(settings);
        });

        // Replace AppDbContext registration to ensure the tests use the isolated SQLite DB
        builder.ConfigureServices(services =>
        {
            // Remove existing AppDbContext registrations (if any)
            var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) || d.ServiceType == typeof(AppDbContext)).ToList();
            foreach (var d in descriptors) services.Remove(d);

            services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={DbPath}"));

            // Build a temporary provider to apply migrations immediately so the test server has schema ready
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ctx.Database.Migrate();
        });

        base.ConfigureWebHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        try
        {
            if (File.Exists(DbPath)) File.Delete(DbPath);
            var shm = DbPath + "-shm";
            var wal = DbPath + "-wal";
            if (File.Exists(shm)) File.Delete(shm);
            if (File.Exists(wal)) File.Delete(wal);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
