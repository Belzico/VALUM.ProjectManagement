using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using VALUM.Application.Projects.Commands;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class StrictIso8601DueDateTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public StrictIso8601DueDateTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        // ACCEPT cases
        [Fact]
        public async Task DueDate_ISO8601WithZ_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59Z" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
        }

        [Fact]
        public async Task DueDate_ISO8601WithFractionalSeconds_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59.123Z" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
        }

        [Fact]
        public async Task DueDate_ISO8601WithPositiveOffset_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59+02:00" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
        }

        [Fact]
        public async Task DueDate_ISO8601WithNegativeOffset_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59-05:30" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
        }

        // REJECT cases
        [Fact]
        public async Task DueDate_SpaceInsteadOfT_Rejected()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31 23:59:59+02:00" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task DueDate_SlashSeparator_Rejected()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026/12/31T23:59:59+02:00" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task DueDate_OffsetWithoutColon_Rejected()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59+02" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task DueDate_NaiveNoTimezone_Rejected()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task DueDate_ImpossibleDate_Rejected()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-02-30T12:00:00Z" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task DueDate_InvalidOffsetRange_Rejected()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "2026-12-31T23:59:59+15:00" };
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
