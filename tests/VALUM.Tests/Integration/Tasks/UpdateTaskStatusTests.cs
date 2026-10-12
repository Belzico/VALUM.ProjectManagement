using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VALUM.Application.Projects.Commands;
using VALUM.Application.Tasks.DTOs;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class UpdateTaskStatusTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public UpdateTaskStatusTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task UpdateStatus_TodoToInProgress_SetsCompletedAtNull()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var updateReq = new { status = 1 }; // InProgress
            var res = await _client.PatchAsJsonAsync($"/api/tasks/{task!.Id}/status", updateReq);
            Assert.Equal(204, (int)res.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_ToDone_SetsCompletedAtUTC()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var updateReq = new { status = 2 }; // Done
            var res = await _client.PatchAsJsonAsync($"/api/tasks/{task!.Id}/status", updateReq);
            Assert.Equal(204, (int)res.StatusCode);

            // Verify CompletedAt is set
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var t = await ctx.Tasks.FindAsync(task.Id);
            Assert.NotNull(t!.CompletedAt);
            Assert.Equal(DateTimeKind.Utc, t.CompletedAt.Value.Kind);
        }

        [Fact]
        public async Task UpdateStatus_DoneToTodo_ClearsCompletedAt()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Set to Done first
            var updateReq1 = new { status = 2 };
            await _client.PatchAsJsonAsync($"/api/tasks/{task!.Id}/status", updateReq1);

            // Then back to Todo
            var updateReq2 = new { status = 0 };
            var res = await _client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", updateReq2);
            Assert.Equal(204, (int)res.StatusCode);

            // Verify CompletedAt is cleared
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var t = await ctx.Tasks.FindAsync(task.Id);
            Assert.Null(t!.CompletedAt);
        }

        [Fact]
        public async Task UpdateStatus_RepeatSameStatus_Returns422()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var updateReq = new { status = 0 }; // Todo (same as initial)
            var res = await _client.PatchAsJsonAsync($"/api/tasks/{task!.Id}/status", updateReq);
            Assert.Equal(422, (int)res.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_NonexistentTask_Returns404()
        {
            var updateReq = new { status = 1 };
            var res = await _client.PatchAsJsonAsync($"/api/tasks/{Guid.NewGuid()}/status", updateReq);
            Assert.Equal(404, (int)res.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_SoftDeletedTask_Returns404()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Soft-delete the task
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var t = await ctx.Tasks.FindAsync(task!.Id);
            t!.IsDeleted = true;
            await ctx.SaveChangesAsync();

            var updateReq = new { status = 1 };
            var res = await _client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", updateReq);
            Assert.Equal(404, (int)res.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_InvalidStatus_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var updateReq = new { status = 99 }; // Invalid
            var res = await _client.PatchAsJsonAsync($"/api/tasks/{task!.Id}/status", updateReq);
            Assert.Equal(400, (int)res.StatusCode);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        private async Task<HttpResponseMessage> PatchAsJsonAsync<T>(string url, T data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
            return await _client.SendAsync(request);
        }
    }
}
