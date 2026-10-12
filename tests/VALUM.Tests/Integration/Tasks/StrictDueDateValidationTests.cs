using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using VALUM.Application.Projects.Commands;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class StrictDueDateValidationTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public StrictDueDateValidationTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task DueDate_ValidISO8601WithZ_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59Z" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
        }

        [Fact]
        public async Task DueDate_ValidISO8601WithFractionalSeconds_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59.123Z" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
        }

        [Fact]
        public async Task DueDate_NaiveDateTime_Rejected()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
            
            var error = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("ISO 8601", error.GetProperty("message").GetString());
        }

        [Fact]
        public async Task DueDate_MalformedOffset_Rejected()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59+25:00" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task DueDate_InvalidFormat_Rejected()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "December 31, 2026" };
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
