using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace VALUM.Tests.Integration.GraphQL
{
    public class GraphQLTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GraphQLTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task ProjectsQuery_WithDefaultPagination_ReturnsData()
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

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"Response was {response.StatusCode}. Body: {responseString}");
        }

        [Fact]
        public async Task ProjectsQuery_WithExplicitPagination_ReturnsData()
        {
            var query = @"
                {
                    projects(page: 2, pageSize: 5) {
                        items {
                            id
                            name
                        }
                        totalCount
                    }
                }";

            var request = new { query = query };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/graphql", content);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task ProjectsQuery_WithPageSize50_ReturnsData()
        {
            var query = @"
                {
                    projects(page: 1, pageSize: 50) {
                        items {
                            id
                        }
                        totalCount
                    }
                }";

            var request = new { query = query };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/graphql", content);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task ProjectsQuery_WithPageSize51_ReturnsError()
        {
            var query = @"
                {
                    projects(page: 1, pageSize: 51) {
                        items {
                            id
                        }
                    }
                }";

            var request = new { query = query };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/graphql", content);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.True(responseJson != null);
            Assert.True(responseJson.RootElement.TryGetProperty("errors", out var errors));
            Assert.True(errors.GetArrayLength() > 0);
        }

        [Fact]
        public async Task ProjectsQuery_WithPage0_ReturnsError()
        {
            var query = @"
                {
                    projects(page: 0, pageSize: 10) {
                        items {
                            id
                        }
                    }
                }";

            var request = new { query = query };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/graphql", content);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.True(responseJson != null);
            Assert.True(responseJson.RootElement.TryGetProperty("errors", out var errors));
            Assert.True(errors.GetArrayLength() > 0);
        }

        [Fact]
        public async Task ProjectsQuery_WithPageSize0_ReturnsError()
        {
            var query = @"
                {
                    projects(page: 1, pageSize: 0) {
                        items {
                            id
                        }
                    }
                }";

            var request = new { query = query };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/graphql", content);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonDocument>(responseString);
            
            Assert.True(responseJson != null);
            Assert.True(responseJson.RootElement.TryGetProperty("errors", out var errors));
            Assert.True(errors.GetArrayLength() > 0);
        }

        [Fact]
        public async Task ProjectQuery_WithNonexistentId_ReturnsData()
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

            var responseString = await response.Content.ReadAsStringAsync();
            
            // GraphQL should return HTTP 200
            Assert.True(response.IsSuccessStatusCode, $"Response was {response.StatusCode}. Body: {responseString}");
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
