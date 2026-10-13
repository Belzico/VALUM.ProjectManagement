using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using VALUM.Application.Common.Pagination;
using VALUM.Domain.Enums;

namespace VALUM.Tests.Integration.Projects
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
        public async Task ProjectsCursor_FirstPageWithoutCursor_ReturnsData()
        {
            var response = await _client.GetAsync("/api/projects/cursor?pageSize=10");
            
            Assert.True(response.IsSuccessStatusCode, $"Response was {response.StatusCode}");
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("items", out var items));
            Assert.True(responseJson.RootElement.TryGetProperty("hasNext", out var hasNext));
            Assert.True(responseJson.RootElement.TryGetProperty("nextCursor", out var nextCursor));
        }

        [Fact]
        public async Task ProjectsCursor_PageSizeValidation_Rejects0()
        {
            var response = await _client.GetAsync("/api/projects/cursor?pageSize=0");
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("message", out var message));
            Assert.Contains("PageSize", message.GetString());
        }

        [Fact]
        public async Task ProjectsCursor_PageSizeValidation_Rejects51()
        {
            var response = await _client.GetAsync("/api/projects/cursor?pageSize=51");
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("message", out var message));
            Assert.Contains("PageSize", message.GetString());
        }

        [Fact]
        public async Task ProjectsCursor_InvalidCursor_Returns400()
        {
            var response = await _client.GetAsync("/api/projects/cursor?cursor=invalid-base64");
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("message", out var message));
        }

        [Fact]
        public async Task ProjectsCursor_MalformedBase64_Returns400()
        {
            var response = await _client.GetAsync("/api/projects/cursor?cursor=!@#$%^&*()");
            
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ProjectsCursor_DefaultPageSize10()
        {
            var response = await _client.GetAsync("/api/projects/cursor");
            
            Assert.True(response.IsSuccessStatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("items", out var items));
        }

        [Fact]
        public async Task ProjectsCursor_PageSize50_Accepted()
        {
            var response = await _client.GetAsync("/api/projects/cursor?pageSize=50");
            
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task ProjectsCursor_StatusFilter_Active()
        {
            var response = await _client.GetAsync("/api/projects/cursor?status=Active");
            
            Assert.True(response.IsSuccessStatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("items", out var items));
        }

        [Fact]
        public async Task ProjectsCursor_StatusFilter_Archived()
        {
            var response = await _client.GetAsync("/api/projects/cursor?status=Archived");
            
            Assert.True(response.IsSuccessStatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("items", out var items));
        }

        [Fact]
        public async Task ProjectsCursor_EmptyResults_HasNextFalse()
        {
            // Create a clean database with no projects
            var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();
            
            var response = await client.GetAsync("/api/projects/cursor");
            
            Assert.True(response.IsSuccessStatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("items", out var items));
            Assert.Equal(0, items.GetArrayLength());
            Assert.True(responseJson.RootElement.TryGetProperty("hasNext", out var hasNext));
            Assert.False(hasNext.GetBoolean());
            
            client.Dispose();
            factory.Dispose();
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
