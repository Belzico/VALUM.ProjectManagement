using System;
// Avoid importing TaskStatus to prevent ambiguity with System.Threading.Tasks.TaskStatus

namespace VALUM.Domain.Entities
{
    public class TaskItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public VALUM.Domain.Enums.TaskPriority Priority { get; set; }
        public VALUM.Domain.Enums.TaskStatus Status { get; set; } = VALUM.Domain.Enums.TaskStatus.Todo;
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
