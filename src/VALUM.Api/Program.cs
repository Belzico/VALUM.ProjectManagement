using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VALUM.Application;
using VALUM.Infrastructure;
using VALUM.Infrastructure.Persistence;
using VALUM.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Read connection string and fail fast if missing
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing required configuration 'ConnectionStrings:DefaultConnection'.");
}

// Register controllers
builder.Services.AddControllers();

// Register application services (MediatR, FluentValidation, pipeline behaviors)
builder.Services.AddApplication();

// Register infrastructure (DbContext, persistence)
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

// Global exception handling must be registered early in the pipeline
app.UseCustomExceptionHandling();

// Map controllers
app.MapControllers();

// Health endpoint for startup verification
app.MapGet("/health", () => Results.Json(new { status = "Healthy" }));

// Apply pending database migrations on startup. Allow exceptions to propagate so failures are visible.
var autoMigrate = app.Configuration.GetValue<bool>("Database:AutoMigrate", true);
if (autoMigrate)
{
    await DatabaseInitializer.ApplyMigrationsAsync(app.Services);
}

// Optionally seed development data. Controlled by configuration and environment.
var seedEnabled = app.Configuration.GetValue<bool>("SeedData:Enabled", false);
await DevelopmentDataSeeder.SeedAsync(app.Services, seedEnabled, app.Environment.IsDevelopment());

app.Run();
