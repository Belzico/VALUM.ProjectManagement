using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;

namespace VALUM.Application.Tasks
{
    public interface ITaskRepository
    {
        Task AddAsync(TaskItem task);
        Task<bool> IsProjectArchivedAsync(Guid projectId);
        Task<List<TaskItem>> GetPageAsync(Guid projectId, VALUM.Domain.Enums.TaskStatus? status, TaskPriority? priority, int skip, int take);
        Task<int> CountAsync(Guid projectId, VALUM.Domain.Enums.TaskStatus? status, TaskPriority? priority);
        Task<TaskItem?> GetByIdAsync(Guid id);
        Task UpdateAsync(TaskItem task);
    }
}
