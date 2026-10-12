using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
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
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e => new { field = e.Key, message = e.Value!.Errors.First().ErrorMessage })
                .ToArray();

            var payload = new
            {
                status = 400,
                error = "ValidationException",
                message = "One or more validation failures have occurred.",
                details = errors
            };

            return new BadRequestObjectResult(payload);
        };
    });

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
