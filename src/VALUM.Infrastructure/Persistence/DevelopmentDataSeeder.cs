using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using TaskStatus = VALUM.Domain.Enums.TaskStatus;

namespace VALUM.Infrastructure.Persistence
{
    public static class DevelopmentDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, bool enabled, bool isDevelopment, CancellationToken cancellationToken = default)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (!enabled || !isDevelopment)
            {
                return;
            }

            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Check for existing records (include soft-deleted tasks)
            var projectsExist = await context.Projects.AnyAsync(cancellationToken).ConfigureAwait(false);
            var tasksExist = await context.Tasks.IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false);

            if (projectsExist || tasksExist)
            {
                return;
            }

            // Create 5 projects and 20 tasks (4 tasks per project)
            var projects = new List<Project>();
            var tasks = new List<TaskItem>();

            // Deterministic composition:
            // - Projects 1-3: Active
            // - Projects 4-5: Archived
            // Tasks: 4 per project, total 20.
            // Status cycle contains Todo, InProgress, Done; InProgress tasks will not be assigned to Archived projects.
            var statusCycle = new[] { TaskStatus.Todo, TaskStatus.InProgress, TaskStatus.Done, TaskStatus.Todo };
            var priorityCycle = new[] { TaskPriority.Low, TaskPriority.Medium, TaskPriority.High, TaskPriority.Medium };

            for (int i = 1; i <= 5; i++)
            {
                var projStatus = i <= 3 ? ProjectStatus.Active : ProjectStatus.Archived;

                var project = new Project
                {
                    Id = Guid.NewGuid(),
                    Name = $"Dev Project {i}",
                    Description = $"Development seeded project {i}",
                    Status = projStatus,
                    CreatedAt = DateTime.UtcNow,
                    OwnerId = Guid.NewGuid()
                };
                projects.Add(project);

                for (int t = 0; t < 4; t++)
                {
                    var status = statusCycle[t % statusCycle.Length];
                    // If project is archived, avoid InProgress tasks
                    if (projStatus == ProjectStatus.Archived && status == TaskStatus.InProgress)
                    {
                        status = TaskStatus.Todo;
                    }

                    var priority = priorityCycle[t % priorityCycle.Length];

                    var createdAt = DateTime.UtcNow;
                    DateTime? due = (t % 3 == 0) ? (DateTime?)null : createdAt.AddDays(7);
                    DateTime? completedAt = null;
                    if (status == TaskStatus.Done)
                    {
                        completedAt = createdAt.AddDays(1);
                    }

                    var task = new TaskItem
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = project.Id,
                        Title = $"Task {i}-{t + 1}",
                        Description = $"Seeded task {i}-{t + 1}",
                        Priority = priority,
                        Status = status,
                        CreatedAt = createdAt,
                        DueDate = due,
                        CompletedAt = completedAt,
                        IsDeleted = false
                    };
                    tasks.Add(task);
                }
            }

            // Use transaction to ensure atomicity
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                context.Projects.AddRange(projects);
                context.Tasks.AddRange(tasks);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                try { await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false); } catch { }
                throw;
            }
        }
    }
}
