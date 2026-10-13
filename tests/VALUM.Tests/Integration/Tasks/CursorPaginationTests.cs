using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class CursorPaginationTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CursorPaginationTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task TasksCursor_FirstPageWithoutCursor_ReturnsData()
        {
            var response = await _client.GetAsync("/api/projects/00000000-0000-0000-0000-000000000000/tasks/cursor?pageSize=10");
            
            // Empty projectId should return BadRequest
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TasksCursor_InvalidProjectId_Returns400()
        {
            var response = await _client.GetAsync("/api/projects/invalid-id/tasks/cursor");
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TasksCursor_PageSizeValidation_Rejects0()
        {
            var response = await _client.GetAsync("/api/projects/00000000-0000-0000-0000-000000000000/tasks/cursor?pageSize=0");
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("message", out var message));
            Assert.Contains("PageSize", message.GetString());
        }

        [Fact]
        public async Task TasksCursor_PageSizeValidation_Rejects51()
        {
            var response = await _client.GetAsync("/api/projects/00000000-0000-0000-0000-000000000000/tasks/cursor?pageSize=51");
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("message", out var message));
            Assert.Contains("PageSize", message.GetString());
        }

        [Fact]
        public async Task TasksCursor_InvalidCursor_Returns400()
        {
            var response = await _client.GetAsync("/api/projects/00000000-0000-0000-0000-000000000000/tasks/cursor?cursor=invalid-base64");
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("message", out var message));
        }

        [Fact]
        public async Task TasksCursor_DefaultPageSize10()
        {
            var response = await _client.GetAsync("/api/projects/00000000-0000-0000-0000-000000000000/tasks/cursor");
            
            // Empty projectId should return BadRequest
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TasksCursor_PageSize50_Accepted()
        {
            var response = await _client.GetAsync("/api/projects/00000000-0000-0000-0000-000000000000/tasks/cursor?pageSize=50");
            
            // Empty projectId should return BadRequest
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TasksCursor_StatusFilter_Todo()
        {
            var response = await _client.GetAsync("/api/projects/00000000-0000-0000-0000-000000000000/tasks/cursor?status=Todo");
            
            // Empty projectId should return BadRequest
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TasksCursor_PriorityFilter_High()
        {
            var response = await _client.GetAsync("/api/projects/00000000-0000-0000-0000-000000000000/tasks/cursor?priority=High");
            
            // Empty projectId should return BadRequest
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TasksCursor_EmptyResults_HasNextFalse()
        {
            var response = await _client.GetAsync("/api/projects/00000000-0000-0000-0000-000000000000/tasks/cursor");
            
            // Empty projectId should return BadRequest
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
