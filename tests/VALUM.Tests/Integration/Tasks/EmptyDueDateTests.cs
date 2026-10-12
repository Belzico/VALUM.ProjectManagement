using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using VALUM.Application.Projects.Commands;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class EmptyDueDateTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public EmptyDueDateTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateTask_MissingDueDate_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
            
            var task = await res.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();
            Assert.Null(task!.DueDate);
        }

        [Fact]
        public async Task CreateTask_NullDueDate_Accepted()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = (string?)null };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(201, (int)res.StatusCode);
            
            var task = await res.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();
            Assert.Null(task!.DueDate);
        }

        [Fact]
        public async Task CreateTask_EmptyDueDate_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "" };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
            
            var error = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, error.GetProperty("status").GetInt32());
            Assert.Equal("ValidationException", error.GetProperty("error").GetString());
        }

        [Fact]
        public async Task CreateTask_WhitespaceDueDate_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1, dueDate = "   " };
            var res = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
            
            var error = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, error.GetProperty("status").GetInt32());
            Assert.Equal("ValidationException", error.GetProperty("error").GetString());
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
