using System;
using MediatR;
using VALUM.Domain.Enums;

namespace VALUM.Application.Tasks.Commands
{
    public class UpdateTaskStatusCommand : IRequest
    {
        public Guid Id { get; set; }
        public VALUM.Domain.Enums.TaskStatus Status { get; set; }
    }
}
