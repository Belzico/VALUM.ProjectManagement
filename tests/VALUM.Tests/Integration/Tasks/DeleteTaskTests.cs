using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VALUM.Application.Projects.Commands;
using VALUM.Application.Tasks.DTOs;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class DeleteTaskTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public DeleteTaskTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Delete_ExistingTask_Returns204()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var res = await _client.DeleteAsync($"/api/tasks/{task!.Id}");
            Assert.Equal(204, (int)res.StatusCode);

            // Verify row still exists with IsDeleted=true
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var t = await ctx.Tasks.FindAsync(task.Id);
            Assert.NotNull(t);
            Assert.True(t!.IsDeleted);
        }

        [Fact]
        public async Task Delete_RepeatedDelete_Returns204()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            await _client.DeleteAsync($"/api/tasks/{task!.Id}");
            var res = await _client.DeleteAsync($"/api/tasks/{task.Id}");
            Assert.Equal(204, (int)res.StatusCode);
        }

        [Fact]
        public async Task Delete_NonexistentTask_Returns404()
        {
            var res = await _client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");
            Assert.Equal(404, (int)res.StatusCode);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
