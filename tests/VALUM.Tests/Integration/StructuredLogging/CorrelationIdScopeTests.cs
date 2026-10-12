using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace VALUM.Tests.Integration.StructuredLogging
{
    public class CorrelationIdScopeTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CorrelationIdScopeTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task ConsecutiveRequests_DoNotLeakCorrelationIds()
        {
            // First request with specific correlation ID
            var correlationId1 = Guid.NewGuid().ToString();
            var request1 = new HttpRequestMessage(HttpMethod.Get, "/health");
            request1.Headers.Add("X-Correlation-ID", correlationId1);
            var response1 = await _client.SendAsync(request1);

            var returnedId1 = response1.Headers.GetValues("X-Correlation-ID").First();
            Assert.Equal(correlationId1, returnedId1);

            // Second request with different correlation ID
            var correlationId2 = Guid.NewGuid().ToString();
            var request2 = new HttpRequestMessage(HttpMethod.Get, "/health");
            request2.Headers.Add("X-Correlation-ID", correlationId2);
            var response2 = await _client.SendAsync(request2);

            var returnedId2 = response2.Headers.GetValues("X-Correlation-ID").First();
            Assert.Equal(correlationId2, returnedId2);

            // Verify they are different
            Assert.NotEqual(correlationId1, correlationId2);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
