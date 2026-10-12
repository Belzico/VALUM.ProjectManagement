using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace VALUM.Tests.Integration.StructuredLogging
{
    public class CorrelationIdMiddlewareTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CorrelationIdMiddlewareTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task RequestWithoutCorrelationId_GeneratesValidGuid()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            var response = await _client.SendAsync(request);

            Assert.True(response.Headers.Contains("X-Correlation-ID"));
            var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
            Assert.True(Guid.TryParse(correlationId, out _));
        }

        [Fact]
        public async Task RequestWithValidGuid_PreservesCorrelationId()
        {
            var expectedId = Guid.NewGuid().ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("X-Correlation-ID", expectedId);
            var response = await _client.SendAsync(request);

            Assert.True(response.Headers.Contains("X-Correlation-ID"));
            var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
            Assert.Equal(expectedId, correlationId);
        }

        [Fact]
        public async Task RequestWithMalformedCorrelationId_GeneratesNewGuid()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("X-Correlation-ID", "not-a-guid");
            var response = await _client.SendAsync(request);

            Assert.True(response.Headers.Contains("X-Correlation-ID"));
            var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
            Assert.True(Guid.TryParse(correlationId, out _));
            Assert.NotEqual("not-a-guid", correlationId);
        }

        [Fact]
        public async Task RequestWithEmptyCorrelationId_GeneratesNewGuid()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("X-Correlation-ID", "");
            var response = await _client.SendAsync(request);

            Assert.True(response.Headers.Contains("X-Correlation-ID"));
            var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
            Assert.True(Guid.TryParse(correlationId, out _));
            Assert.NotEqual("", correlationId);
        }

        [Fact]
        public async Task RequestWithInvalidGuid_GeneratesNewGuid()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("X-Correlation-ID", Guid.Empty.ToString());
            var response = await _client.SendAsync(request);

            Assert.True(response.Headers.Contains("X-Correlation-ID"));
            var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
            Assert.True(Guid.TryParse(correlationId, out _));
            Assert.NotEqual(Guid.Empty.ToString(), correlationId);
        }

        [Fact]
        public async Task ValidationError_ReturnsCorrelationId()
        {
            var correlationId = Guid.NewGuid().ToString();
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/projects");
            request.Headers.Add("X-Correlation-ID", correlationId);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(request);

            Assert.True(response.Headers.Contains("X-Correlation-ID"));
            var returnedId = response.Headers.GetValues("X-Correlation-ID").First();
            Assert.Equal(correlationId, returnedId);
        }

        [Fact]
        public async Task NotFoundError_ReturnsCorrelationId()
        {
            var correlationId = Guid.NewGuid().ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/projects/00000000-0000-0000-0000-000000000000");
            request.Headers.Add("X-Correlation-ID", correlationId);
            var response = await _client.SendAsync(request);

            Assert.True(response.Headers.Contains("X-Correlation-ID"));
            var returnedId = response.Headers.GetValues("X-Correlation-ID").First();
            Assert.Equal(correlationId, returnedId);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
