using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VALUM.Application.Projects.Commands;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class CreateTaskTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CreateTaskTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Create_ValidTaskInActiveProject_Returns201()
        {
            // Create an active project
            var createProject = new CreateProjectCommand { Name = "Active Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            // Create a task
            var createTask = new
            {
                title = "Test Task",
                description = "Test Description",
                priority = 1, // Medium
                dueDate = "2026-12-31T23:59:59Z"
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
        }

        [Fact]
        public async Task Create_TaskInArchivedProject_Returns422()
        {
            // Create a project
            var createProject = new CreateProjectCommand { Name = "Archived Project", OwnerId = Guid.NewGuid() };
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

            // Try to create a task
            var createTask = new
            {
                title = "Test Task",
                priority = 1
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", createTask);
            Assert.Equal(422, (int)res.StatusCode);
        }

        [Fact]
        public async Task Create_NonexistentProject_Returns404()
        {
            var createTask = new
            {
                title = "Test Task",
                priority = 1
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{Guid.NewGuid()}/tasks", createTask);
            Assert.Equal(404, (int)res.StatusCode);
        }

        [Fact]
        public async Task Create_EmptyTitle_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Active Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new
            {
                title = "",
                priority = 1
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task Create_WhitespaceTitle_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Active Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new
            {
                title = "   ",
                priority = 1
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task Create_Title101Characters_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Active Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new
            {
                title = new string('a', 101),
                priority = 1
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task Create_Title100Characters_Returns201()
        {
            var createProject = new CreateProjectCommand { Name = "Active Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new
            {
                title = new string('a', 100),
                priority = 1
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
        }

        [Fact]
        public async Task Create_DueDateWithZ_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Active Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new
            {
                title = "Test Task",
                priority = 1,
                dueDate = "2026-12-31T23:59:59Z"
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
        }

        [Fact]
        public async Task Create_DueDateWithPositiveOffset_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Active Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new
            {
                title = "Test Task",
                priority = 1,
                dueDate = "2026-12-31T23:59:59+05:00"
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
        }

        [Fact]
        public async Task Create_DueDateWithNegativeOffset_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Active Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new
            {
                title = "Test Task",
                priority = 1,
                dueDate = "2026-12-31T23:59:59-08:00"
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
        }

        [Fact]
        public async Task Create_DueDateWithoutTimezone_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Active Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new
            {
                title = "Test Task",
                priority = 1,
                dueDate = "2026-12-31T23:59:59"
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task Create_InvalidDueDate_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Active Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new
            {
                title = "Test Task",
                priority = 1,
                dueDate = "invalid-date"
            };

            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
