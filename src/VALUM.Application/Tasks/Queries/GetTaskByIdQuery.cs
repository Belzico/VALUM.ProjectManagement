using System;
using MediatR;
using VALUM.Application.Tasks.DTOs;

namespace VALUM.Application.Tasks.Queries
{
    public class GetTaskByIdQuery : IRequest<TaskDto>
    {
        public Guid Id { get; set; }
    }
}
