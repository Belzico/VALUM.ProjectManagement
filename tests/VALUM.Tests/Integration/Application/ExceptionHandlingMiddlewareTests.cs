using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using VALUM.Api.Extensions;
using VALUM.Application.Exceptions;
using VALUM.Domain.Exceptions;
using Xunit;

namespace VALUM.Tests.Integration.Application
{
    public class ExceptionHandlingMiddlewareTests
    {
        private TestServer CreateServer(RequestDelegate handler)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services => { })
                .Configure(app =>
                {
                    app.UseCustomExceptionHandling();
                    app.Run(handler);
                });

            return new TestServer(builder);
        }

        [Fact]
        public async Task ValidationException_maps_to_400_with_details()
        {
            var server = CreateServer(ctx => throw new VALUM.Application.Exceptions.ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Name","Required") }));
            var client = server.CreateClient();
            var res = await client.GetAsync("/");
            Assert.Equal(400, (int)res.StatusCode);
            var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("ValidationException", json.RootElement.GetProperty("error").GetString());
            Assert.True(json.RootElement.GetProperty("details").GetArrayLength() > 0);
        }

        [Fact]
        public async Task NotFoundException_maps_to_404()
        {
            var server = CreateServer(ctx => throw new NotFoundException("Not here"));
            var client = server.CreateClient();
            var res = await client.GetAsync("/");
            Assert.Equal(404, (int)res.StatusCode);
            var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("NotFoundException", json.RootElement.GetProperty("error").GetString());
        }

        [Fact]
        public async Task DomainException_maps_to_422()
        {
            var server = CreateServer(ctx => throw new DomainException("Business rule"));
            var client = server.CreateClient();
            var res = await client.GetAsync("/");
            Assert.Equal(422, (int)res.StatusCode);
            var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal(422, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("DomainException", json.RootElement.GetProperty("error").GetString());
        }

        [Fact]
        public async Task Exception_maps_to_500_and_no_details()
        {
            var server = CreateServer(ctx => throw new System.Exception("boom"));
            var client = server.CreateClient();
            var res = await client.GetAsync("/");
            Assert.Equal(500, (int)res.StatusCode);
            var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("Exception", json.RootElement.GetProperty("error").GetString());
            Assert.Equal(0, json.RootElement.GetProperty("details").GetArrayLength());
        }
    }
}
