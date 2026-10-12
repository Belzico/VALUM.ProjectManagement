using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VALUM.Application.Projects.Commands;
using VALUM.Application.Tasks.DTOs;
using VALUM.Domain.Entities;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class SoftDeletePersistenceTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public SoftDeletePersistenceTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task SoftDelete_PhysicalRowRemainsInSQLite()
        {
            var createProject = new CreateProjectCommand { Name = "Test Project", OwnerId = Guid.NewGuid() };
            var projectRes = await _client.PostAsJsonAsync("/api/projects", createProject);
            var project = await projectRes.Content.ReadFromJsonAsync<VALUM.Application.Projects.DTOs.ProjectDto>();

            var createTask = new { title = "Test Task", priority = 1 };
            var taskRes = await _client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks", createTask);
            var task = await taskRes.Content.ReadFromJsonAsync<VALUM.Application.Tasks.DTOs.TaskDto>();

            // Soft-delete
            await _client.DeleteAsync($"/api/tasks/{task!.Id}");

            // Verify physical row still exists using IgnoreQueryFilters
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);
            
            var physicalTask = await ctx.Tasks.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == task.Id);
            Assert.NotNull(physicalTask);
            Assert.True(physicalTask!.IsDeleted);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
