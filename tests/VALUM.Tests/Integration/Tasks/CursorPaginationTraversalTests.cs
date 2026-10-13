using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VALUM.Domain.Entities;
using VALUM.Infrastructure.Persistence;
using Xunit;

namespace VALUM.Tests.Integration.Tasks
{
    public class CursorPaginationTraversalTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly VALUM.Infrastructure.Tasks.TaskRepository _repository;

        public CursorPaginationTraversalTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=task_cursor_traversal_test.db")
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
            _repository = new VALUM.Infrastructure.Tasks.TaskRepository(_context);
        }

        [Fact]
        public async Task TasksCursor_MultiPageTraversal_FiveTasksPageSize2()
        {
            // Setup: Create project and 5 tasks
            var projectId = Guid.NewGuid();
            var baseTime = DateTime.UtcNow;
            
            // Create project first (foreign key constraint)
            _context.Projects.Add(new Project
            {
                Id = projectId,
                Name = "Test Project",
                CreatedAt = baseTime,
                OwnerId = Guid.NewGuid()
            });
            
            var taskIds = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Guid.Parse("55555555-5555-5555-5555-555555555555")
            };

            for (int i = 0; i < 5; i++)
            {
                _context.Tasks.Add(new TaskItem
                {
                    Id = taskIds[i],
                    ProjectId = projectId,
                    Title = $"Task {i}",
                    Description = $"Description {i}",
                    CreatedAt = baseTime.AddHours(i),
                    Priority = VALUM.Domain.Enums.TaskPriority.Low,
                    Status = VALUM.Domain.Enums.TaskStatus.Todo
                });
            }
            await _context.SaveChangesAsync();

            // Page 1 (pageSize=2, fetch 3 for lookahead)
            var page1 = await _repository.GetPageByCursorAsync(projectId, null, null, null, null, 3);
            Assert.Equal(3, page1.Count);
            
            var page1Items = page1.Take(2).ToList();
            Assert.Equal(2, page1Items.Count);
            Assert.Equal(taskIds[0], page1Items[0].Id);
            Assert.Equal(taskIds[1], page1Items[1].Id);

            // Page 2
            var cursorCreatedAt = page1Items[1].CreatedAt;
            var cursorId = page1Items[1].Id;
            var page2 = await _repository.GetPageByCursorAsync(projectId, null, null, cursorCreatedAt, cursorId, 3);
            Assert.Equal(3, page2.Count);
            
            var page2Items = page2.Take(2).ToList();
            Assert.Equal(2, page2Items.Count);
            Assert.Equal(taskIds[2], page2Items[0].Id);
            Assert.Equal(taskIds[3], page2Items[1].Id);

            // Page 3
            cursorCreatedAt = page2Items[1].CreatedAt;
            cursorId = page2Items[1].Id;
            var page3 = await _repository.GetPageByCursorAsync(projectId, null, null, cursorCreatedAt, cursorId, 3);
            Assert.Equal(1, page3.Count);
            
            Assert.Equal(taskIds[4], page3[0].Id);

            // Verify all IDs appear exactly once
            var allReturnedIds = page1Items.Concat(page2Items).Concat(page3).Select(t => t.Id).ToList();
            Assert.Equal(5, allReturnedIds.Count);
            Assert.Equal(5, allReturnedIds.Distinct().Count());
        }

        [Fact]
        public async Task TasksCursor_SoftDeleted_Excluded()
        {
            var projectId = Guid.NewGuid();
            var baseTime = DateTime.UtcNow;

            // Create project first
            _context.Projects.Add(new Project
            {
                Id = projectId,
                Name = "Test Project",
                CreatedAt = baseTime,
                OwnerId = Guid.NewGuid()
            });

            // Create 3 tasks, one soft-deleted
            _context.Tasks.AddRange(
                new TaskItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), ProjectId = projectId, Title = "T1", CreatedAt = baseTime, Priority = VALUM.Domain.Enums.TaskPriority.Low, Status = VALUM.Domain.Enums.TaskStatus.Todo, IsDeleted = false },
                new TaskItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), ProjectId = projectId, Title = "T2", CreatedAt = baseTime.AddHours(1), Priority = VALUM.Domain.Enums.TaskPriority.Low, Status = VALUM.Domain.Enums.TaskStatus.Todo, IsDeleted = true },
                new TaskItem { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), ProjectId = projectId, Title = "T3", CreatedAt = baseTime.AddHours(2), Priority = VALUM.Domain.Enums.TaskPriority.Low, Status = VALUM.Domain.Enums.TaskStatus.Todo, IsDeleted = false }
            );
            await _context.SaveChangesAsync();

            var tasks = await _repository.GetPageByCursorAsync(projectId, null, null, null, null, 10);
            
            Assert.Equal(2, tasks.Count);
            Assert.DoesNotContain(tasks, t => t.Id == Guid.Parse("22222222-2222-2222-2222-222222222222"));
        }

        [Fact]
        public async Task TasksCursor_CombinedFilters_StatusAndPriority()
        {
            var projectId = Guid.NewGuid();
            var baseTime = DateTime.UtcNow;

            // Create project first
            _context.Projects.Add(new Project
            {
                Id = projectId,
                Name = "Test Project",
                CreatedAt = baseTime,
                OwnerId = Guid.NewGuid()
            });

            _context.Tasks.AddRange(
                new TaskItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), ProjectId = projectId, Title = "T1", CreatedAt = baseTime, Priority = VALUM.Domain.Enums.TaskPriority.Low, Status = VALUM.Domain.Enums.TaskStatus.Todo },
                new TaskItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), ProjectId = projectId, Title = "T2", CreatedAt = baseTime.AddHours(1), Priority = VALUM.Domain.Enums.TaskPriority.High, Status = VALUM.Domain.Enums.TaskStatus.Todo },
                new TaskItem { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), ProjectId = projectId, Title = "T3", CreatedAt = baseTime.AddHours(2), Priority = VALUM.Domain.Enums.TaskPriority.High, Status = VALUM.Domain.Enums.TaskStatus.InProgress }
            );
            await _context.SaveChangesAsync();

            var tasks = await _repository.GetPageByCursorAsync(projectId, VALUM.Domain.Enums.TaskStatus.Todo, VALUM.Domain.Enums.TaskPriority.High, null, null, 10);
            
            Assert.Single(tasks);
            Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), tasks[0].Id);
        }

        [Fact]
        public async Task TasksCursor_ProjectIsolation()
        {
            var project1Id = Guid.NewGuid();
            var project2Id = Guid.NewGuid();
            var baseTime = DateTime.UtcNow;

            // Create projects first
            _context.Projects.AddRange(
                new Project { Id = project1Id, Name = "Project 1", CreatedAt = baseTime, OwnerId = Guid.NewGuid() },
                new Project { Id = project2Id, Name = "Project 2", CreatedAt = baseTime, OwnerId = Guid.NewGuid() }
            );

            _context.Tasks.AddRange(
                new TaskItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), ProjectId = project1Id, Title = "T1", CreatedAt = baseTime, Priority = VALUM.Domain.Enums.TaskPriority.Low, Status = VALUM.Domain.Enums.TaskStatus.Todo },
                new TaskItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), ProjectId = project2Id, Title = "T2", CreatedAt = baseTime, Priority = VALUM.Domain.Enums.TaskPriority.Low, Status = VALUM.Domain.Enums.TaskStatus.Todo }
            );
            await _context.SaveChangesAsync();

            var project1Tasks = await _repository.GetPageByCursorAsync(project1Id, null, null, null, null, 10);
            var project2Tasks = await _repository.GetPageByCursorAsync(project2Id, null, null, null, null, 10);
            
            Assert.Single(project1Tasks);
            Assert.Single(project2Tasks);
            Assert.NotEqual(project1Tasks[0].Id, project2Tasks[0].Id);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
