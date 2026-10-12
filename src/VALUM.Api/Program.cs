using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using VALUM.Application;
using VALUM.Infrastructure;
using VALUM.Infrastructure.Persistence;
using VALUM.Api.Extensions;
using VALUM.Api.Logging;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "VALUM.Api")
    .CreateLogger();

builder.Host.UseSerilog();

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

            var failedFields = errors.Select(e => e.field).ToArray();
            Log.Warning("Model binding failed. Failed fields: {FailedFields}. Failures: {FailureCount}", failedFields, errors.Length);

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

// Configure log cleanup options
builder.Services.Configure<LogCleanupOptions>(builder.Configuration.GetSection("LogCleanup"));

// Register log cleanup background service
builder.Services.AddHostedService<LogCleanupBackgroundService>();

var app = builder.Build();

// Correlation ID middleware must be early in the pipeline
app.UseCorrelationId();

// Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null)
            return LogEventLevel.Error;

        var statusCode = httpContext.Response.StatusCode;
        if (statusCode >= 500)
            return LogEventLevel.Error;
        if (statusCode >= 400)
            return LogEventLevel.Warning;
        return LogEventLevel.Information;
    };
});

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

try
{
    Log.Information("Starting VALUM.Api");
    await app.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}