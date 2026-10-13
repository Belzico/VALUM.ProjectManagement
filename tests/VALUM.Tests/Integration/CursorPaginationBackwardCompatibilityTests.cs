using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VALUM.Tests.Integration
{
    public class CursorPaginationBackwardCompatibilityTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CursorPaginationBackwardCompatibilityTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task OffsetPagination_GetProjects_StillWorks()
        {
            var response = await _client.GetAsync("/api/projects?page=1&pageSize=10");
            
            Assert.True(response.IsSuccessStatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("items", out var items));
            Assert.True(responseJson.RootElement.TryGetProperty("page", out var page));
            Assert.True(responseJson.RootElement.TryGetProperty("pageSize", out var pageSize));
            Assert.True(responseJson.RootElement.TryGetProperty("totalCount", out var totalCount));
            Assert.True(responseJson.RootElement.TryGetProperty("totalPages", out var totalPages));
            Assert.True(responseJson.RootElement.TryGetProperty("hasPrevious", out var hasPrevious));
            Assert.True(responseJson.RootElement.TryGetProperty("hasNext", out var hasNext));
        }

        [Fact]
        public async Task OffsetPagination_GetTasks_StillWorks()
        {
            var response = await _client.GetAsync("/api/projects/invalid-guid/tasks?page=1&pageSize=10");
            
            // Invalid projectId should return BadRequest
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GraphQL_Projects_StillWorks()
        {
            var query = @"
                {
                    projects(page: 1, pageSize: 10) {
                        items {
                            id
                            name
                            status
                        }
                        totalCount
                    }
                }";

            var request = new { query = query };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/graphql", content);

            Assert.True(response.IsSuccessStatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("data", out var data));
        }

        [Fact]
        public async Task GraphQL_Project_StillWorks()
        {
            var query = @"
                {
                    project(id: ""00000000-0000-0000-0000-000000000000"") {
                        project {
                            id
                            name
                            status
                        }
                        summary {
                            totalTasks
                            completedTasks
                            inProgressTasks
                            pendingTasks
                        }
                    }
                }";

            var request = new { query = query };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/graphql", content);

            Assert.True(response.IsSuccessStatusCode);
            
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.NotNull(responseJson);
            Assert.True(responseJson.RootElement.TryGetProperty("data", out var data));
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
