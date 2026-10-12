using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VALUM.Application.Projects.Commands;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class RequestErrorHandlingTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public RequestErrorHandlingTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateTask_MalformedJson_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");
            var res = await _client.PostAsync($"/api/projects/{project!.Id}/tasks", content);
            
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task CreateTask_NullRequestBody_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var content = new StringContent("null", Encoding.UTF8, "application/json");
            var res = await _client.PostAsync($"/api/projects/{project!.Id}/tasks", content);
            
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_MalformedJson_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/tasks/{task!.Id}/status") { Content = content };
            var res = await _client.SendAsync(req);
            
            Assert.Equal(400, (int)res.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_NullRequestBody_Returns400()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            var content = new StringContent("null", Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/tasks/{task!.Id}/status") { Content = content };
            var res = await _client.SendAsync(req);
            
            Assert.Equal(400, (int)res.StatusCode);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
