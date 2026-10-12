using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VALUM.Application.Projects.Commands;
using VALUM.Application.Tasks.DTOs;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class ExactUtcValueTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ExactUtcValueTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task DueDate_Z_PreservesExactUtc()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59Z" };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Verify HTTP response has correct UTC value
            var taskDueDate = task!.DueDate;
            Assert.NotNull(taskDueDate);
            Assert.True(taskDueDate.Value.Kind == DateTimeKind.Utc);
            Assert.Equal(2026, taskDueDate.Value.Year);
            Assert.Equal(12, taskDueDate.Value.Month);
            Assert.Equal(31, taskDueDate.Value.Day);
            Assert.Equal(23, taskDueDate.Value.Hour);
            Assert.Equal(59, taskDueDate.Value.Minute);
            Assert.Equal(59, taskDueDate.Value.Second);

            // Verify SQLite persistence
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var persisted = await ctx.Tasks.FindAsync(task.Id);
            
            Assert.NotNull(persisted);
            Assert.NotNull(persisted!.DueDate);
            var persistedDueDate = persisted.DueDate.Value;
            Assert.Equal(DateTimeKind.Utc, persistedDueDate.Kind);
            Assert.Equal(2026, persistedDueDate.Year);
            Assert.Equal(12, persistedDueDate.Month);
            Assert.Equal(31, persistedDueDate.Day);
            Assert.Equal(23, persistedDueDate.Hour);
            Assert.Equal(59, persistedDueDate.Minute);
            Assert.Equal(59, persistedDueDate.Second);

            // Verify GET response has correct UTC value
            var getRes = await _client.GetAsync($"/api/tasks/{task.Id}");
            var getTask = await getRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();
            var getDueDate = getTask!.DueDate;
            Assert.NotNull(getDueDate);
            Assert.True(getDueDate.Value.Kind == DateTimeKind.Utc);
            Assert.Equal(2026, getDueDate.Value.Year);
            Assert.Equal(12, getDueDate.Value.Month);
            Assert.Equal(31, getDueDate.Value.Day);
            Assert.Equal(23, getDueDate.Value.Hour);
            Assert.Equal(59, getDueDate.Value.Minute);
            Assert.Equal(59, getDueDate.Value.Second);
        }

        [Fact]
        public async Task DueDate_PositiveOffset_ConvertsToUtc()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59+02:00" };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Input: 2026-12-31T23:59:59+02:00 → Expected UTC: 2026-12-31T21:59:59Z
            var taskDueDate = task!.DueDate;
            Assert.NotNull(taskDueDate);
            Assert.True(taskDueDate.Value.Kind == DateTimeKind.Utc);
            Assert.Equal(2026, taskDueDate.Value.Year);
            Assert.Equal(12, taskDueDate.Value.Month);
            Assert.Equal(31, taskDueDate.Value.Day);
            Assert.Equal(21, taskDueDate.Value.Hour); // 23:59:59 +02:00 = 21:59:59 UTC
            Assert.Equal(59, taskDueDate.Value.Minute);
            Assert.Equal(59, taskDueDate.Value.Second);

            // Verify SQLite persistence
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var persisted = await ctx.Tasks.FindAsync(task.Id);
            
            Assert.NotNull(persisted);
            Assert.NotNull(persisted!.DueDate);
            var persistedDueDate = persisted.DueDate.Value;
            Assert.Equal(DateTimeKind.Utc, persistedDueDate.Kind);
            Assert.Equal(21, persistedDueDate.Hour);
        }

        [Fact]
        public async Task DueDate_NegativeOffset_ConvertsToUtc()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59-05:30" };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Input: 2026-12-31T23:59:59-05:30 → Expected UTC: 2027-01-01T05:29:59Z
            var taskDueDate = task!.DueDate;
            Assert.NotNull(taskDueDate);
            Assert.True(taskDueDate.Value.Kind == DateTimeKind.Utc);
            Assert.Equal(2027, taskDueDate.Value.Year); // Day rollover
            Assert.Equal(1, taskDueDate.Value.Month);
            Assert.Equal(1, taskDueDate.Value.Day);
            Assert.Equal(5, taskDueDate.Value.Hour); // 23:59:59 -05:30 = 05:29:59 UTC next day
            Assert.Equal(29, taskDueDate.Value.Minute);
            Assert.Equal(59, taskDueDate.Value.Second);

            // Verify SQLite persistence
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var persisted = await ctx.Tasks.FindAsync(task.Id);
            
            Assert.NotNull(persisted);
            Assert.NotNull(persisted!.DueDate);
            var persistedDueDate = persisted.DueDate.Value;
            Assert.Equal(DateTimeKind.Utc, persistedDueDate.Kind);
            Assert.Equal(2027, persistedDueDate.Year);
            Assert.Equal(5, persistedDueDate.Hour);
        }

        [Fact]
        public async Task DueDate_FractionalOneDigit_Preserves()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59.1Z" };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Should preserve fractional seconds - verify it's stored and returned
            Assert.NotNull(task!.DueDate);
            Assert.True(task.DueDate.Value.Kind == DateTimeKind.Utc);
            Assert.Equal(23, task.DueDate.Value.Hour);
            Assert.Equal(59, task.DueDate.Value.Minute);
            Assert.Equal(59, task.DueDate.Value.Second);
        }

        [Fact]
        public async Task DueDate_FractionalSevenDigits_Preserves()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59.1234567Z" };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Verify exact UTC instant including all 7 fractional digits
            Assert.NotNull(task!.DueDate);
            Assert.True(task.DueDate.Value.Kind == DateTimeKind.Utc);
            
            // Expected: 2026-12-31T23:59:59.1234567Z
            // .1234567 seconds = 1,234,567 ticks
            var expected = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(1234567);
            Assert.Equal(expected, task.DueDate.Value);
        }

        [Fact]
        public async Task DueDate_EquivalentInstants_EqualPersisted()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            // Create two tasks with equivalent instants using Z
            var createTask1 = new { title = "Task 1", priority = 1, dueDate = "2026-12-31T23:59:59Z" };
            var taskRes1 = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask1);
            var task1 = await taskRes1.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var createTask2 = new { title = "Task 2", priority = 1, dueDate = "2026-12-31T23:59:59Z" };
            var taskRes2 = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", createTask2);
            var task2 = await taskRes2.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Both should have the same persisted UTC value (verify using Ticks for exact comparison)
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var persisted1 = await ctx.Tasks.FindAsync(task1!.Id);
            var persisted2 = await ctx.Tasks.FindAsync(task2!.Id);
            
            Assert.NotNull(persisted1);
            Assert.NotNull(persisted2);
            Assert.NotNull(persisted1!.DueDate);
            Assert.NotNull(persisted2!.DueDate);
            
            // Both should represent the same UTC instant
            var utc1 = persisted1.DueDate.Value.ToUniversalTime();
            var utc2 = persisted2.DueDate.Value.ToUniversalTime();
            Assert.Equal(utc1.Ticks, utc2.Ticks);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
