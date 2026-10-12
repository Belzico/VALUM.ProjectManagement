using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using VALUM.Application.Projects.Commands;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class InvalidEnumValuesTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public InvalidEnumValuesTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateTask_InvalidPriority99_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 99 };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
            
            var error = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, error.GetProperty("status").GetInt32());
            Assert.Equal("ValidationException", error.GetProperty("error").GetString());
        }

        [Fact]
        public async Task GetTasks_InvalidStatus99_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var res = await _client.GetAsync($"/api/projects/{project!.Id}/tasks?status=99");
            Assert.Equal(400, (int)res.StatusCode);
            
            var error = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, error.GetProperty("status").GetInt32());
            Assert.Equal("ValidationException", error.GetProperty("error").GetString());
        }

        [Fact]
        public async Task GetTasks_InvalidPriority99_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var res = await _client.GetAsync($"/api/projects/{project!.Id}/tasks?priority=99");
            Assert.Equal(400, (int)res.StatusCode);
            
            var error = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, error.GetProperty("status").GetInt32());
            Assert.Equal("ValidationException", error.GetProperty("error").GetString());
        }

        [Fact]
        public async Task CreateTask_ValidPriorityValues_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            // Test priority 0 (Low)
            var createTask0 = new { title = "Test Task", priority = 0 };
            var res0 = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask0);
            Assert.Equal(201, (int)res0.StatusCode);

            // Test priority 1 (Medium)
            var createTask1 = new { title = "Test Task 2", priority = 1 };
            var res1 = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", createTask1);
            Assert.Equal(201, (int)res1.StatusCode);

            // Test priority 2 (High)
            var createTask2 = new { title = "Test Task 3", priority = 2 };
            var res2 = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", createTask2);
            Assert.Equal(201, (int)res2.StatusCode);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
