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

namespace VALUM.Tests.Integration.Projects
{
    public class ArchiveProjectTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ArchiveProjectTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Archive_ActiveProjectWithoutTasks_Returns204()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project!.Id}/archive");
            var res = await _client.SendAsync(req);

            Assert.Equal(204, (int)res.StatusCode);
            Assert.Empty(await res.Content.ReadAsStringAsync());

            // Verify persisted status
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var persisted = await ctx.Projects.FindAsync(project.Id);
            Assert.Equal(ProjectStatus.Archived, persisted!.Status);
        }

        [Fact]
        public async Task Archive_ActiveProjectWithTodoTasks_Returns204()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);

            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project.Id}/archive");
            var res = await _client.SendAsync(req);

            Assert.Equal(204, (int)res.StatusCode);

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var persisted = await ctx.Projects.FindAsync(project.Id);
            Assert.Equal(ProjectStatus.Archived, persisted!.Status);
        }

        [Fact]
        public async Task Archive_ActiveProjectWithDoneTasks_Returns204()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var updateReq = new { status = 2 };
            var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/tasks/{task!.Id}/status")
            {
                Content = new StringContent(JsonSerializer.Serialize(updateReq), Encoding.UTF8, "application/json")
            };
            await _client.SendAsync(patchReq);

            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project.Id}/archive");
            var res = await _client.SendAsync(req);

            Assert.Equal(204, (int)res.StatusCode);

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var persisted = await ctx.Projects.FindAsync(project.Id);
            Assert.Equal(ProjectStatus.Archived, persisted!.Status);
        }

        [Fact]
        public async Task Archive_ProjectWithInProgressTask_Returns422()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var updateReq = new { status = 1 };
            var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/tasks/{task!.Id}/status")
            {
                Content = new StringContent(JsonSerializer.Serialize(updateReq), Encoding.UTF8, "application/json")
            };
            await _client.SendAsync(patchReq);

            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project.Id}/archive");
            var res = await _client.SendAsync(req);

            Assert.Equal(422, (int)res.StatusCode);

            var error = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(422, error.GetProperty("status").GetInt32());
            Assert.Equal("DomainException", error.GetProperty("error").GetString());
            Assert.Contains("Cannot archive a project with tasks in progress", error.GetProperty("message").GetString());

            // Verify project remains Active
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var persisted = await ctx.Projects.FindAsync(project.Id);
            Assert.Equal(ProjectStatus.Active, persisted!.Status);
        }

        [Fact]
        public async Task Archive_NonexistentProject_Returns404()
        {
            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{Guid.NewGuid()}/archive");
            var res = await _client.SendAsync(req);

            Assert.Equal(404, (int)res.StatusCode);
        }

        [Fact]
        public async Task Archive_MalformedGuid_Returns400()
        {
            var req = new HttpRequestMessage(HttpMethod.Patch, "/api/projects/not-a-guid/archive");
            var res = await _client.SendAsync(req);

            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task Archive_EmptyGuid_Returns400()
        {
            var req = new HttpRequestMessage(HttpMethod.Patch, "/api/projects/00000000-0000-0000-0000-000000000000/archive");
            var res = await _client.SendAsync(req);

            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task Archive_AlreadyArchivedProject_Returns204()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var req1 = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project!.Id}/archive");
            await _client.SendAsync(req1);

            var req2 = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project.Id}/archive");
            var res = await _client.SendAsync(req2);

            Assert.Equal(204, (int)res.StatusCode);
        }

        [Fact]
        public async Task Archive_SoftDeletedInProgressTasks_AllowsArchiving()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var updateReq = new { status = 1 };
            var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/tasks/{task!.Id}/status")
            {
                Content = new StringContent(JsonSerializer.Serialize(updateReq), Encoding.UTF8, "application/json")
            };
            await _client.SendAsync(patchReq);

            // Soft-delete the InProgress task
            await _client.DeleteAsync($"/api/tasks/{task.Id}");

            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project.Id}/archive");
            var res = await _client.SendAsync(req);

            Assert.Equal(204, (int)res.StatusCode);

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var persisted = await ctx.Projects.FindAsync(project.Id);
            Assert.Equal(ProjectStatus.Archived, persisted!.Status);
        }

        [Fact]
        public async Task Archive_RN01_CannotCreateTaskInArchivedProject()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project!.Id}/archive");
            await _client.SendAsync(req);

            var createTask = new { title = "Test Task", priority = 1 };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", createTask);

            Assert.Equal(422, (int)res.StatusCode);
        }

        [Fact]
        public async Task Archive_RN01_CanCreateTaskInOtherActiveProject()
        {
            var createProject1 = new CreateProjectCommand { Name = "Test Project 1", OwnerId = Guid.NewGuid() };
            var projectRes1 = await _client.PostAsJsonAsync("/api/projects", createProject1);
            var project1 = await projectRes1.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createProject2 = new CreateProjectCommand { Name = "Test Project 2", OwnerId = Guid.NewGuid() };
            var projectRes2 = await _client.PostAsJsonAsync("/api/projects", createProject2);
            var project2 = await projectRes2.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            // Archive project 1
            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project1!.Id}/archive");
            await _client.SendAsync(req);

            // Should still be able to create task in project 2
            var createTask = new { title = "Test Task", priority = 1 };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project2!.Id}/tasks", createTask);

            Assert.Equal(201, (int)res.StatusCode);
        }

        [Fact]
        public async Task Archive_ExistingTasksRemainReadable()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project.Id}/archive");
            await _client.SendAsync(req);

            // Should still be able to read the task
            var getRes = await _client.GetAsync($"/api/tasks/{task!.Id}");
            Assert.Equal(200, (int)getRes.StatusCode);
        }

        [Fact]
        public async Task Archive_RN05_ProjectSummaryReflectsArchivedStatus()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);

            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project.Id}/archive");
            await _client.SendAsync(req);

            var getRes = await _client.GetAsync($"/api/projects/{project.Id}");
            var result = await getRes.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(1, result.GetProperty("project").GetProperty("status").GetInt32());
            Assert.Equal(1, result.GetProperty("summary").GetProperty("totalTasks").GetInt32());
        }

        [Fact]
        public async Task Archive_RN06_ArchivedIncludedInStatusFilter()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project!.Id}/archive");
            await _client.SendAsync(req);

            var getRes = await _client.GetAsync("/api/projects?status=1");
            var result = await getRes.Content.ReadFromJsonAsync<JsonElement>();
            var items = result.GetProperty("items");

            Assert.True(items.GetArrayLength() > 0);
        }

        [Fact]
        public async Task Archive_RN06_ArchivedExcludedFromActiveFilter()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project!.Id}/archive");
            await _client.SendAsync(req);

            var getRes = await _client.GetAsync("/api/projects?status=0");
            var result = await getRes.Content.ReadFromJsonAsync<JsonElement>();
            var items = result.GetProperty("items");

            var containsArchived = false;
            foreach (var item in items.EnumerateArray())
            {
                if (item.GetProperty("id").GetString() == project.Id.ToString())
                {
                    containsArchived = true;
                    break;
                }
            }
            Assert.False(containsArchived);
        }

        [Fact]
        public async Task Archive_Idempotency_ArchivedProjectWithInProgressTask_Returns204()
        {
            // Arrange: Create Active project with Todo task
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Act: Archive the project
            var archiveReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project.Id}/archive");
            var archiveRes = await _client.SendAsync(archiveReq);
            Assert.Equal(204, (int)archiveRes.StatusCode);

            // Update existing task to InProgress (allowed for archived projects)
            var updateReq = new { status = 1 };
            var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/tasks/{task!.Id}/status")
            {
                Content = new StringContent(JsonSerializer.Serialize(updateReq), Encoding.UTF8, "application/json")
            };
            await _client.SendAsync(patchReq);

            // Call archive again on already-archived project with InProgress task
            var archiveReq2 = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project.Id}/archive");
            var archiveRes2 = await _client.SendAsync(archiveReq2);

            // Assert: Should return 204 (idempotent), not 422
            Assert.Equal(204, (int)archiveRes2.StatusCode);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
