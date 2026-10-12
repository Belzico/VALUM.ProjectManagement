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
    public class ArchivedProjectRegressionTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ArchivedProjectRegressionTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task ArchivedProject_CanReadExistingTasks()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            // Create a task
            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Archive the project
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var proj = await ctx.Projects.FindAsync(project.Id);
            proj!.Status = ProjectStatus.Archived;
            await ctx.SaveChangesAsync();

            // Should still be able to read the task
            var res = await _client.GetAsync($"/api/tasks/{task!.Id}");
            Assert.Equal(200, (int)res.StatusCode);
        }

        [Fact]
        public async Task ArchivedProject_CanUpdateTaskStatus()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            // Create a task
            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Archive the project
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var proj = await ctx.Projects.FindAsync(project.Id);
            proj!.Status = ProjectStatus.Archived;
            await ctx.SaveChangesAsync();

            // Should still be able to update task status
            var updateReq = new { status = 1 };
            var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/tasks/{task!.Id}/status")
            {
                Content = new StringContent(JsonSerializer.Serialize(updateReq), Encoding.UTF8, "application/json")
            };
            var res = await _client.SendAsync(patchReq);
            Assert.Equal(204, (int)res.StatusCode);
        }

        [Fact]
        public async Task ArchivedProject_CanSoftDeleteTask()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            // Create a task
            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Archive the project
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var proj = await ctx.Projects.FindAsync(project.Id);
            proj!.Status = ProjectStatus.Archived;
            await ctx.SaveChangesAsync();

            // Should still be able to soft-delete the task
            var res = await _client.DeleteAsync($"/api/tasks/{task!.Id}");
            Assert.Equal(204, (int)res.StatusCode);
        }

        [Fact]
        public async Task ArchivedProject_CannotCreateNewTask()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            // Archive the project
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var proj = await ctx.Projects.FindAsync(project!.Id);
            proj!.Status = ProjectStatus.Archived;
            await ctx.SaveChangesAsync();

            // Should NOT be able to create a new task
            var createTask = new { title = "New Task", priority = 1 };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", createTask);
            Assert.Equal(422, (int)res.StatusCode);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
