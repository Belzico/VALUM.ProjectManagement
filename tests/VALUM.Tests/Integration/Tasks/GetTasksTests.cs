using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VALUM.Application.Projects.Commands;
using VALUM.Application.Tasks.DTOs;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using Xunit;
using System.Text.Json;

namespace VALUM.Tests.Integration.Tasks
{
    public class GetTasksTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GetTasksTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Get_DefaultPagination_Returns200()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var res = await _client.GetAsync($"/api/projects/{project!.Id}/tasks");
            Assert.Equal(200, (int)res.StatusCode);
        }

        [Fact]
        public async Task Get_NonexistentProject_Returns404()
        {
            var res = await _client.GetAsync($"/api/projects/{Guid.NewGuid()}/tasks");
            Assert.Equal(404, (int)res.StatusCode);
        }

        [Fact]
        public async Task Get_InvalidPage_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var res = await _client.GetAsync($"/api/projects/{project!.Id}/tasks?page=0");
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task Get_InvalidPageSize_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var res = await _client.GetAsync($"/api/projects/{project!.Id}/tasks?pageSize=51");
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task Get_WithStatusFilter_ReturnsFiltered()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var res = await _client.GetAsync($"/api/projects/{project!.Id}/tasks?status=0");
            Assert.Equal(200, (int)res.StatusCode);
        }

        [Fact]
        public async Task Get_WithPriorityFilter_ReturnsFiltered()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var res = await _client.GetAsync($"/api/projects/{project!.Id}/tasks?priority=1");
            Assert.Equal(200, (int)res.StatusCode);
        }

        [Fact]
        public async Task Get_SoftDeletedTasksExcluded()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            // Create a task
            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var taskDto = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Soft-delete the task
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var t = await ctx.Tasks.FindAsync(taskDto!.Id);
            t!.IsDeleted = true;
            await ctx.SaveChangesAsync();

            // Verify it's excluded from list
            var res = await _client.GetAsync($"/api/projects/{project.Id}/tasks");
            Assert.Equal(200, (int)res.StatusCode);
            var result = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, result.GetProperty("items").GetArrayLength());
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
