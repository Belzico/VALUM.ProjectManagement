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
    public class ProjectSummaryIntegrationTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProjectSummaryIntegrationTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task ProjectSummary_ReflectsTaskCreation()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            // Check initial summary
            var summaryRes1 = await _client.GetAsync($"/api/projects/{project!.Id}");
            var summary1 = await summaryRes1.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, summary1.GetProperty("summary").GetProperty("totalTasks").GetInt32());

            // Create a task
            var createTask = new { title = "Test Task", priority = 1 };
            await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", createTask);

            // Check summary after creation
            var summaryRes2 = await _client.GetAsync($"/api/projects/{project.Id}");
            var summary2 = await summaryRes2.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, summary2.GetProperty("summary").GetProperty("totalTasks").GetInt32());
            Assert.Equal(1, summary2.GetProperty("summary").GetProperty("pendingTasks").GetInt32());
        }

        [Fact]
        public async Task ProjectSummary_ReflectsStatusChange()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Update to Done
            var updateReq = new { status = 2 };
            var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/tasks/{task!.Id}/status")
            {
                Content = new StringContent(JsonSerializer.Serialize(updateReq), Encoding.UTF8, "application/json")
            };
            await _client.SendAsync(patchReq);

            // Check summary
            var summaryRes = await _client.GetAsync($"/api/projects/{project.Id}");
            var summary = await summaryRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, summary.GetProperty("summary").GetProperty("completedTasks").GetInt32());
            Assert.Equal(0, summary.GetProperty("summary").GetProperty("pendingTasks").GetInt32());
        }

        [Fact]
        public async Task ProjectSummary_ExcludesSoftDeletedTasks()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Soft-delete task
            await _client.DeleteAsync($"/api/tasks/{task!.Id}");

            // Check summary
            var summaryRes = await _client.GetAsync($"/api/projects/{project.Id}");
            var summary = await summaryRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(0, summary.GetProperty("summary").GetProperty("totalTasks").GetInt32());
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
