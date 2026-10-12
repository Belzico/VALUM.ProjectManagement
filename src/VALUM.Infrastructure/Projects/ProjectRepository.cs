using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VALUM.Application.Projects;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using VALUM.Infrastructure.Persistence;

namespace VALUM.Infrastructure.Projects
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _context;

        public ProjectRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Project project)
        {
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
        }

        // Read operations used by application queries
        public async Task<int> CountAsync(ProjectStatus? status = null)
        {
            var q = _context.Projects.AsQueryable();
            if (status.HasValue) q = q.Where(p => p.Status == status.Value);
            return await q.CountAsync();
        }

        public async Task<List<Project>> GetPageAsync(ProjectStatus? status, int skip, int take)
        {
            var q = _context.Projects.AsQueryable();
            if (status.HasValue) q = q.Where(p => p.Status == status.Value);
            q = q.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id);
            return await q.Skip(skip).Take(take).ToListAsync();
        }

        public async Task<Project?> GetByIdAsync(Guid id)
        {
            return await _context.Projects.FindAsync(id);
        }

        public async Task<ProjectTaskSummary?> GetTaskSummaryAsync(Guid projectId)
        {
            var q = _context.Tasks.Where(t => t.ProjectId == projectId && !t.IsDeleted);
            var total = await q.CountAsync();
            if (total == 0) return new ProjectTaskSummary { Total = 0, Completed = 0, InProgress = 0, Pending = 0 };
            var completed = await q.CountAsync(t => t.Status == VALUM.Domain.Enums.TaskStatus.Done);
            var inProgress = await q.CountAsync(t => t.Status == VALUM.Domain.Enums.TaskStatus.InProgress);
            var pending = await q.CountAsync(t => t.Status == VALUM.Domain.Enums.TaskStatus.Todo);
            return new ProjectTaskSummary { Total = total, Completed = completed, InProgress = inProgress, Pending = pending };
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Projects.AnyAsync(p => p.Id == id);
        }

        public async Task UpdateAsync(Project project)
        {
            _context.Projects.Update(project);
            await _context.SaveChangesAsync();
        }
    }
}
