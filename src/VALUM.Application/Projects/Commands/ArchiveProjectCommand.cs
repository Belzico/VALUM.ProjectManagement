using MediatR;

namespace VALUM.Application.Projects.Commands
{
    public record ArchiveProjectCommand : IRequest
    {
        public Guid Id { get; init; }
    }
}
