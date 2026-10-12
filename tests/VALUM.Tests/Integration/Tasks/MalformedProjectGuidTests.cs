using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class MalformedProjectGuidTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MalformedProjectGuidTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateTask_MalformedProjectGuid_Returns400()
        {
            var createTask = new { title = "Test Task", priority = 1 };
            var res = await _client.PostAsJsonAsync("/api/projects/not-a-guid/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
            
            var error = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, error.GetProperty("status").GetInt32());
            Assert.Equal("ValidationException", error.GetProperty("error").GetString());
        }

        [Fact]
        public async Task CreateTask_EmptyProjectGuid_Returns400()
        {
            var createTask = new { title = "Test Task", priority = 1 };
            var res = await _client.PostAsJsonAsync("/api/projects/00000000-0000-0000-0000-000000000000/tasks", createTask);
            Assert.Equal(400, (int)res.StatusCode);
            
            var error = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, error.GetProperty("status").GetInt32());
            Assert.Equal("ValidationException", error.GetProperty("error").GetString());
        }

        [Fact]
        public async Task GetTasks_MalformedProjectGuid_Returns400()
        {
            var res = await _client.GetAsync("/api/projects/not-a-guid/tasks");
            Assert.Equal(400, (int)res.StatusCode);
            
            var error = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, error.GetProperty("status").GetInt32());
            Assert.Equal("ValidationException", error.GetProperty("error").GetString());
        }

        [Fact]
        public async Task GetTasks_EmptyProjectGuid_Returns400()
        {
            var res = await _client.GetAsync("/api/projects/00000000-0000-0000-0000-000000000000/tasks");
            Assert.Equal(400, (int)res.StatusCode);
            
            var error = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, error.GetProperty("status").GetInt32());
            Assert.Equal("ValidationException", error.GetProperty("error").GetString());
        }

        [Fact]
        public async Task GetTasks_ValidNonexistentProjectGuid_Returns404()
        {
            var nonexistentGuid = Guid.NewGuid();
            var res = await _client.GetAsync($"/api/projects/{nonexistentGuid}/tasks");
            Assert.Equal(404, (int)res.StatusCode);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
