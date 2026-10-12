using System;
using MediatR;

namespace VALUM.Application.Tasks.Commands
{
    public class DeleteTaskCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
