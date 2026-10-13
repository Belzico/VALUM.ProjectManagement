using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;

namespace VALUM.Application.Projects
{
    public interface IProjectRepository
    {
        Task AddAsync(Project project);

        Task<int> CountAsync(ProjectStatus? status = null);

        Task<List<Project>> GetPageAsync(ProjectStatus? status, int skip, int take);

        Task<Project?> GetByIdAsync(Guid id);

        Task<ProjectTaskSummary?> GetTaskSummaryAsync(Guid projectId);

        Task<bool> ExistsAsync(Guid id);

        Task UpdateAsync(Project project);

        Task<List<Project>> GetPageByCursorAsync(ProjectStatus? status, DateTime? cursorCreatedAt, Guid? cursorId, int take);
    }
}
