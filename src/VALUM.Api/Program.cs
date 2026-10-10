using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VALUM.Application;

var builder = WebApplication.CreateBuilder(args);

// Register controllers
builder.Services.AddControllers();

// Register application services (MediatR, FluentValidation, pipeline behaviors)
builder.Services.AddApplication();

var app = builder.Build();

// Map controllers
app.MapControllers();

// Health endpoint for startup verification
app.MapGet("/health", () => Results.Json(new { status = "Healthy" }));

app.Run();
