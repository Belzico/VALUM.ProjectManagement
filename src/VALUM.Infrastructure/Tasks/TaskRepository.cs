using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VALUM.Application.Tasks;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using VALUM.Infrastructure.Persistence;

namespace VALUM.Infrastructure.Tasks
{
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TaskItem task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsProjectArchivedAsync(Guid projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            return project != null && project.Status == ProjectStatus.Archived;
        }

        public async Task<bool> HasActiveInProgressTasksAsync(Guid projectId)
        {
            return await _context.Tasks
                .Where(t => t.ProjectId == projectId && t.Status == VALUM.Domain.Enums.TaskStatus.InProgress && !t.IsDeleted)
                .AnyAsync();
        }

        public async Task<List<TaskItem>> GetPageAsync(Guid projectId, VALUM.Domain.Enums.TaskStatus? status, TaskPriority? priority, int skip, int take)
        {
            var q = _context.Tasks.Where(t => t.ProjectId == projectId && !t.IsDeleted);
            
            if (status.HasValue)
                q = q.Where(t => t.Status == status.Value);
            
            if (priority.HasValue)
                q = q.Where(t => t.Priority == priority.Value);
            
            q = q.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id);
            
            return await q.Skip(skip).Take(take).ToListAsync();
        }

        public async Task<int> CountAsync(Guid projectId, VALUM.Domain.Enums.TaskStatus? status, TaskPriority? priority)
        {
            var q = _context.Tasks.Where(t => t.ProjectId == projectId && !t.IsDeleted);
            
            if (status.HasValue)
                q = q.Where(t => t.Status == status.Value);
            
            if (priority.HasValue)
                q = q.Where(t => t.Priority == priority.Value);
            
            return await q.CountAsync();
        }

        public async Task<TaskItem?> GetByIdAsync(Guid id)
        {
            return await _context.Tasks.FindAsync(id);
        }

        public async Task UpdateAsync(TaskItem task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
        }
    }
}
