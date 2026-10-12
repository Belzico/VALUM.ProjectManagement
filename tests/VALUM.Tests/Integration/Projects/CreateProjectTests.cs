using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using VALUM.Api;
using VALUM.Application.Projects.Commands;
using Xunit;

namespace VALUM.Tests.Integration.Projects
{
    public class CreateProjectTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CreateProjectTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        private class ValidationErrorResponse
        {
            public int Status { get; set; }
            public string Error { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public FieldError[] Details { get; set; } = Array.Empty<FieldError>();
        }

        private class FieldError
        {
            public string Field { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        [Fact]
        public async Task Post_ValidProject_ReturnsCreated()
        {
            var client = _client;
            var cmd = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var res = await client.PostAsJsonAsync("/api/projects", cmd);
            Assert.Equal(201, (int)res.StatusCode);
            var dto = await res.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();
            Assert.NotNull(dto);
            Assert.NotEqual(Guid.Empty, dto!.Id);
            Assert.Equal("Test Project", dto.Name);

            // verify persisted in the same isolated DB using EF
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctxEf = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            var persisted = await ctxEf.Projects.FindAsync(dto.Id);
            Assert.NotNull(persisted);
            Assert.Equal("Test Project", persisted!.Name);
        }

        [Fact]
        public async Task Post_InvalidName_Returns400()
        {
            var client = _client;
            var cmd = new CreateProjectCommand { Name = "", OwnerId = Guid.NewGuid() };
            var res = await client.PostAsJsonAsync("/api/projects", cmd);
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.NotEmpty(error.Details);
            Assert.Contains(error.Details, d => d.Field == "Name");
        }

        [Fact]
        public async Task Post_WhitespaceName_Returns400()
        {
            var client = _client;
            var cmd = new CreateProjectCommand { Name = "   ", OwnerId = Guid.NewGuid() };
            var res = await client.PostAsJsonAsync("/api/projects", cmd);
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.NotEmpty(error.Details);
            Assert.Contains(error.Details, d => d.Field == "Name");
        }

        [Fact]
        public async Task Post_NameLengthBoundary_Returns400On81()
        {
            var client = _client;
            var name = new string('a', 81);
            var cmd = new CreateProjectCommand { Name = name, OwnerId = Guid.NewGuid() };
            var res = await client.PostAsJsonAsync("/api/projects", cmd);
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.NotEmpty(error.Details);
            Assert.Contains(error.Details, d => d.Field == "Name");
        }

        [Fact]
        public async Task Post_EmptyOwnerId_Returns400()
        {
            var client = _client;
            var cmd = new CreateProjectCommand { Name = "OK", OwnerId = Guid.Empty };
            var res = await client.PostAsJsonAsync("/api/projects", cmd);
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.NotEmpty(error.Details);
            Assert.Contains(error.Details, d => d.Field == "OwnerId");
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
