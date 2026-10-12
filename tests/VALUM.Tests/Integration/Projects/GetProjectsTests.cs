using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using VALUM.Application.Projects.Commands;
using VALUM.Application.Projects.DTOs;
using VALUM.Application.Projects.Queries;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using Xunit;

namespace VALUM.Tests.Integration.Projects
{
    public class GetProjectsTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GetProjectsTests()
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
        public async Task Get_DefaultPagination_Returns200AndPagedResult()
        {
            var client = _client;
            for (int i = 1; i <= 15; i++)
            {
                var cmd = new CreateProjectCommand { Name = $"P{i}", OwnerId = Guid.NewGuid() };
                await client.PostAsJsonAsync("/api/projects", cmd);
            }

            var res = await client.GetAsync("/api/projects");
            Assert.Equal(200, (int)res.StatusCode);
            var paged = await res.Content.ReadFromJsonAsync<VALUM.Application.Common.Pagination.PagedResult<ProjectDto>>();
            Assert.NotNull(paged);
            Assert.Equal(1, paged.Page);
            Assert.Equal(10, paged.PageSize);
            Assert.Equal(15, paged.TotalCount);
            Assert.Equal(2, paged.TotalPages);
            Assert.True(paged.HasNext);
        }

        [Fact]
        public async Task Get_WithStatusFilter_ReturnsFilteredResults()
        {
            var client = _client;
            var ownerId = Guid.NewGuid();

            var activeCmd = new CreateProjectCommand { Name = "Active Project", OwnerId = ownerId };
            await client.PostAsJsonAsync("/api/projects", activeCmd);

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);

            var archivedProject = new Project
            {
                Name = "Archived Project",
                OwnerId = ownerId,
                Status = ProjectStatus.Archived,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Projects.Add(archivedProject);
            await ctx.SaveChangesAsync();

            var res = await client.GetAsync("/api/projects?status=Active");
            Assert.Equal(200, (int)res.StatusCode);
            var paged = await res.Content.ReadFromJsonAsync<VALUM.Application.Common.Pagination.PagedResult<ProjectDto>>();
            Assert.NotNull(paged);
            Assert.Equal(1, paged.TotalCount);
            Assert.Equal("Active Project", paged!.Items[0].Name);
        }

        [Fact]
        public async Task Get_WithPageTwo_ReturnsSecondPage()
        {
            var client = _client;
            for (int i = 1; i <= 15; i++)
            {
                var cmd = new CreateProjectCommand { Name = $"P{i}", OwnerId = Guid.NewGuid() };
                await client.PostAsJsonAsync("/api/projects", cmd);
            }

            var res = await client.GetAsync("/api/projects?page=2&pageSize=10");
            Assert.Equal(200, (int)res.StatusCode);
            var paged = await res.Content.ReadFromJsonAsync<VALUM.Application.Common.Pagination.PagedResult<ProjectDto>>();
            Assert.NotNull(paged);
            Assert.Equal(2, paged.Page);
            Assert.Equal(10, paged.PageSize);
            Assert.Equal(15, paged.TotalCount);
            Assert.Equal(5, paged!.Items.Count);
            Assert.False(paged.HasNext);
            Assert.True(paged.HasPrevious);
        }

        [Fact]
        public async Task Get_WithInvalidPageSize_Returns400()
        {
            var res = await _client.GetAsync("/api/projects?pageSize=51");
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.NotEmpty(error.Details);
            Assert.Contains(error.Details, d => d.Field == "PageSize");
        }

        [Fact]
        public async Task Get_WithNegativePage_Returns400()
        {
            var res = await _client.GetAsync("/api/projects?page=-1");
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.NotEmpty(error.Details);
            Assert.Contains(error.Details, d => d.Field == "Page");
        }

        [Fact]
        public async Task Get_WithZeroPageSize_Returns400()
        {
            var res = await _client.GetAsync("/api/projects?pageSize=0");
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.NotEmpty(error.Details);
            Assert.Contains(error.Details, d => d.Field == "PageSize");
        }

        [Fact]
        public async Task Get_WithZeroPage_Returns400()
        {
            var res = await _client.GetAsync("/api/projects?page=0");
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.NotEmpty(error.Details);
            Assert.Contains(error.Details, d => d.Field == "Page");
        }

        [Fact]
        public async Task Get_WithMaxPageSize_Returns50Items()
        {
            var client = _client;
            for (int i = 1; i <= 60; i++)
            {
                var cmd = new CreateProjectCommand { Name = $"P{i}", OwnerId = Guid.NewGuid() };
                await client.PostAsJsonAsync("/api/projects", cmd);
            }

            var res = await client.GetAsync("/api/projects?pageSize=50");
            Assert.Equal(200, (int)res.StatusCode);
            var paged = await res.Content.ReadFromJsonAsync<VALUM.Application.Common.Pagination.PagedResult<ProjectDto>>();
            Assert.NotNull(paged);
            Assert.Equal(50, paged!.Items.Count);
            Assert.Equal(60, paged.TotalCount);
        }

        [Fact]
        public async Task Get_OrderByCreatedAtAndId_ReturnsStableOrder()
        {
            var client = _client;
            var ownerId = Guid.NewGuid();

            var cmd1 = new CreateProjectCommand { Name = "Project A", OwnerId = ownerId };
            await client.PostAsJsonAsync("/api/projects", cmd1);

            await Task.Delay(10);

            var cmd2 = new CreateProjectCommand { Name = "Project B", OwnerId = ownerId };
            await client.PostAsJsonAsync("/api/projects", cmd2);

            var res = await client.GetAsync("/api/projects");
            Assert.Equal(200, (int)res.StatusCode);
            var paged = await res.Content.ReadFromJsonAsync<VALUM.Application.Common.Pagination.PagedResult<ProjectDto>>();
            Assert.NotNull(paged);
            Assert.Equal(2, paged!.Items.Count);
            Assert.Equal("Project A", paged.Items[0].Name);
            Assert.Equal("Project B", paged.Items[1].Name);
        }

        [Fact]
        public async Task Get_InvalidPageParameter_Returns400WithJsonError()
        {
            var res = await _client.GetAsync("/api/projects?page=abc");
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.NotEmpty(error.Details);
            Assert.Contains(error.Details, d => d.Field == "page");
        }

        [Fact]
        public async Task Get_InvalidStatusParameter_Returns400WithJsonError()
        {
            var res = await _client.GetAsync("/api/projects?status=InvalidStatus");
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.NotEmpty(error.Details);
            Assert.Contains(error.Details, d => d.Field == "status");
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
