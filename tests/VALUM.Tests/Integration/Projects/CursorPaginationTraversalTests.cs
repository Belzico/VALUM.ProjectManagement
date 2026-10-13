using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Projects;
using VALUM.Domain.Entities;
using VALUM.Infrastructure.Persistence;
using Xunit;

namespace VALUM.Tests.Integration.Projects
{
    public class CursorPaginationTraversalTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IProjectRepository _repository;

        public CursorPaginationTraversalTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=cursor_traversal_test.db")
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
            _repository = new VALUM.Infrastructure.Projects.ProjectRepository(_context);
        }

        [Fact]
        public async Task ProjectsCursor_MultiPageTraversal_FiveProjectsPageSize2()
        {
            // Setup: Create 5 projects with deterministic CreatedAt
            var baseTime = DateTime.UtcNow;
            var projectIds = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Guid.Parse("55555555-5555-5555-5555-555555555555")
            };

            for (int i = 0; i < 5; i++)
            {
                _context.Projects.Add(new Project
                {
                    Id = projectIds[i],
                    Name = $"Project {i}",
                    Description = $"Description {i}",
                    CreatedAt = baseTime.AddHours(i),
                    OwnerId = Guid.NewGuid()
                });
            }
            await _context.SaveChangesAsync();

            // Page 1 (pageSize=2, fetch 3 for lookahead)
            var page1 = await _repository.GetPageByCursorAsync(null, null, null, 3);
            Assert.Equal(3, page1.Count); // 2 items + 1 lookahead
            
            var page1Items = page1.Take(2).ToList();
            Assert.Equal(2, page1Items.Count);
            Assert.Equal(projectIds[0], page1Items[0].Id);
            Assert.Equal(projectIds[1], page1Items[1].Id);

            // Page 2 (using cursor from last item of page 1)
            var cursorCreatedAt = page1Items[1].CreatedAt;
            var cursorId = page1Items[1].Id;
            var page2 = await _repository.GetPageByCursorAsync(null, cursorCreatedAt, cursorId, 3);
            Assert.Equal(3, page2.Count);
            
            var page2Items = page2.Take(2).ToList();
            Assert.Equal(2, page2Items.Count);
            Assert.Equal(projectIds[2], page2Items[0].Id);
            Assert.Equal(projectIds[3], page2Items[1].Id);

            // Page 3 (using cursor from last item of page 2)
            cursorCreatedAt = page2Items[1].CreatedAt;
            cursorId = page2Items[1].Id;
            var page3 = await _repository.GetPageByCursorAsync(null, cursorCreatedAt, cursorId, 3);
            Assert.Equal(1, page3.Count); // Only 1 remaining, no lookahead
            
            Assert.Equal(projectIds[4], page3[0].Id);

            // Verify all IDs appear exactly once
            var allReturnedIds = page1Items.Concat(page2Items).Concat(page3).Select(p => p.Id).ToList();
            Assert.Equal(5, allReturnedIds.Count);
            Assert.Equal(5, allReturnedIds.Distinct().Count());
        }

        [Fact]
        public async Task ProjectsCursor_SameCreatedAt_TieBreaker()
        {
            // Setup: Create 4 projects with identical CreatedAt
            var sameTime = DateTime.UtcNow;
            var projectIds = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Guid.Parse("44444444-4444-4444-4444-444444444444")
            };

            for (int i = 0; i < 4; i++)
            {
                _context.Projects.Add(new Project
                {
                    Id = projectIds[i],
                    Name = $"Project {i}",
                    Description = $"Description {i}",
                    CreatedAt = sameTime,
                    OwnerId = Guid.NewGuid()
                });
            }
            await _context.SaveChangesAsync();

            // Page 1 (pageSize=2)
            var page1 = await _repository.GetPageByCursorAsync(null, null, null, 3);
            var page1Items = page1.Take(2).ToList();
            Assert.Equal(2, page1Items.Count);

            // Page 2
            var cursorCreatedAt = page1Items[1].CreatedAt;
            var cursorId = page1Items[1].Id;
            var page2 = await _repository.GetPageByCursorAsync(null, cursorCreatedAt, cursorId, 3);
            var page2Items = page2.Take(2).ToList();
            Assert.Equal(2, page2Items.Count);

            // Verify all 4 projects returned
            var allReturnedIds = page1Items.Concat(page2Items).Select(p => p.Id).ToList();
            Assert.Equal(4, allReturnedIds.Count);
            Assert.Equal(4, allReturnedIds.Distinct().Count());

            // Verify deterministic ordering by Id
            var sortedIds = allReturnedIds.OrderBy(id => id).ToList();
            Assert.Equal(sortedIds, allReturnedIds);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
