using System;
using MediatR;
using VALUM.Domain.Enums;
using VALUM.Application.Tasks.DTOs;

namespace VALUM.Application.Tasks.Commands
{
    public class CreateTaskCommand : IRequest<TaskDto>
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
