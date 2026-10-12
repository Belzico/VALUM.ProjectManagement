using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VALUM.Application.Projects.Commands;
using VALUM.Application.Projects.DTOs;
using VALUM.Application.Projects.Queries;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using Xunit;

namespace VALUM.Tests.Integration.Projects
{
    public class GetProjectByIdTests : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GetProjectByIdTests()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        private class ProjectWithSummary
        {
            public ProjectDto Project { get; set; } = null!;
            public TaskSummaryDto Summary { get; set; } = null!;
        }

        private class TaskSummaryDto
        {
            public int TotalTasks { get; set; }
            public int CompletedTasks { get; set; }
            public int InProgressTasks { get; set; }
            public int PendingTasks { get; set; }
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

        private class NotFoundErrorResponse
        {
            public int Status { get; set; }
            public string Error { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public object[] Details { get; set; } = Array.Empty<object>();
        }

        [Fact]
        public async Task GetById_ExistingProjectWithNoTasks_ReturnsCountsZero()
        {
            var create = new CreateProjectCommand { Name = "X", OwnerId = Guid.NewGuid() };
            var post = await _client.PostAsJsonAsync("/api/projects", create);
            var dto = await post.Content.ReadFromJsonAsync<ProjectDto>();
            var res = await _client.GetAsync($"/api/projects/{dto!.Id}");
            Assert.Equal(200, (int)res.StatusCode);
            var obj = await res.Content.ReadFromJsonAsync<ProjectWithSummary>();
            Assert.NotNull(obj);
            Assert.Equal(0, obj!.Summary.TotalTasks);
            Assert.Equal(0, obj.Summary.CompletedTasks);
            Assert.Equal(0, obj.Summary.InProgressTasks);
            Assert.Equal(0, obj.Summary.PendingTasks);
        }

        [Fact]
        public async Task GetById_ExistingProjectWithTasks_ReturnsCorrectSummary()
        {
            var create = new CreateProjectCommand { Name = "Project With Tasks", OwnerId = Guid.NewGuid() };
            var post = await _client.PostAsJsonAsync("/api/projects", create);
            var dto = await post.Content.ReadFromJsonAsync<ProjectDto>();

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);

            var todoTask = new TaskItem
            {
                ProjectId = dto!.Id,
                Title = "Todo Task",
                Priority = TaskPriority.Medium,
                Status = VALUM.Domain.Enums.TaskStatus.Todo
            };
            var inProgressTask = new TaskItem
            {
                ProjectId = dto.Id,
                Title = "In Progress Task",
                Priority = TaskPriority.High,
                Status = VALUM.Domain.Enums.TaskStatus.InProgress
            };
            var doneTask = new TaskItem
            {
                ProjectId = dto.Id,
                Title = "Done Task",
                Priority = TaskPriority.Low,
                Status = VALUM.Domain.Enums.TaskStatus.Done
            };

            ctx.Tasks.AddRange(todoTask, inProgressTask, doneTask);
            await ctx.SaveChangesAsync();

            var res = await _client.GetAsync($"/api/projects/{dto.Id}");
            Assert.Equal(200, (int)res.StatusCode);
            var obj = await res.Content.ReadFromJsonAsync<ProjectWithSummary>();
            Assert.NotNull(obj);
            Assert.Equal(3, obj!.Summary.TotalTasks);
            Assert.Equal(1, obj.Summary.CompletedTasks);
            Assert.Equal(1, obj.Summary.InProgressTasks);
            Assert.Equal(1, obj.Summary.PendingTasks);
        }

        [Fact]
        public async Task GetById_WithSoftDeletedTasks_ExcludesDeletedFromSummary()
        {
            var create = new CreateProjectCommand { Name = "Project With Deleted Tasks", OwnerId = Guid.NewGuid() };
            var post = await _client.PostAsJsonAsync("/api/projects", create);
            var dto = await post.Content.ReadFromJsonAsync<ProjectDto>();

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<VALUM.Infrastructure.Persistence.AppDbContext>();
            options.UseSqlite($"Data Source={_factory.DbPath}");
            var optionsBuilt = options.Options;
            await using var ctx = new VALUM.Infrastructure.Persistence.AppDbContext(optionsBuilt);

            var activeTask = new TaskItem
            {
                ProjectId = dto!.Id,
                Title = "Active Task",
                Priority = TaskPriority.Medium,
                Status = VALUM.Domain.Enums.TaskStatus.Todo
            };
            var deletedTask = new TaskItem
            {
                ProjectId = dto.Id,
                Title = "Deleted Task",
                Priority = TaskPriority.High,
                Status = VALUM.Domain.Enums.TaskStatus.Done,
                IsDeleted = true
            };

            ctx.Tasks.AddRange(activeTask, deletedTask);
            await ctx.SaveChangesAsync();

            var res = await _client.GetAsync($"/api/projects/{dto.Id}");
            Assert.Equal(200, (int)res.StatusCode);
            var obj = await res.Content.ReadFromJsonAsync<ProjectWithSummary>();
            Assert.NotNull(obj);
            Assert.Equal(1, obj!.Summary.TotalTasks);
            Assert.Equal(0, obj.Summary.CompletedTasks);
            Assert.Equal(0, obj.Summary.InProgressTasks);
            Assert.Equal(1, obj.Summary.PendingTasks);
        }

        [Fact]
        public async Task GetById_NonExistentProject_Returns404()
        {
            var res = await _client.GetAsync($"/api/projects/{Guid.NewGuid()}");
            Assert.Equal(404, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<NotFoundErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(404, error!.Status);
            Assert.Equal("NotFoundException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
        }

        [Fact]
        public async Task GetById_EmptyGuid_Returns400()
        {
            var res = await _client.GetAsync("/api/projects/00000000-0000-0000-0000-000000000000");
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.NotEmpty(error.Details);
            Assert.Contains(error.Details, d => d.Field == "Id");
        }

        [Fact]
        public async Task GetById_InvalidGuid_Returns400()
        {
            var res = await _client.GetAsync("/api/projects/not-a-guid");
            Assert.Equal(400, (int)res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
            Assert.NotNull(error);
            Assert.Equal(400, error!.Status);
            Assert.Equal("ValidationException", error.Error);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Details);
            Assert.Empty(error.Details);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
