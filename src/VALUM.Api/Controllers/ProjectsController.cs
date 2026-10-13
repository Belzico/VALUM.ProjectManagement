using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VALUM.Application.Projects.Commands;
using VALUM.Application.Projects.DTOs;
using VALUM.Application.Projects.Queries;
using VALUM.Domain.Enums;
using VALUM.Application.Common.Pagination;

namespace VALUM.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProjectsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectCommand command)
        {
            var dto = await _mediator.Send(command);
            return Created($"/api/projects/{dto.Id}", dto);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] ProjectStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetProjectsQuery { Status = status, Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            if (!Guid.TryParse(id, out var guidId))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Invalid project ID format.",
                    details = Array.Empty<object>()
                });
            }

            var query = new GetProjectByIdQuery { Id = guidId };
            var result = await _mediator.Send(query);

            return Ok(new
            {
                project = result.Project,
                summary = new
                {
                    totalTasks = result.Summary.Total,
                    completedTasks = result.Summary.Completed,
                    inProgressTasks = result.Summary.InProgress,
                    pendingTasks = result.Summary.Pending
                }
            });
        }

        [HttpPatch("{id}/archive")]
        public async Task<IActionResult> Archive([FromRoute] string id)
        {
            if (!Guid.TryParse(id, out var guidId))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Invalid project ID format.",
                    details = Array.Empty<object>()
                });
            }

            if (guidId == Guid.Empty)
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Project ID cannot be empty.",
                    details = Array.Empty<object>()
                });
            }

            var command = new ArchiveProjectCommand { Id = guidId };
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpGet("cursor")]
        public async Task<IActionResult> GetByCursor([FromQuery] string? cursor, [FromQuery] int pageSize = 10, [FromQuery] ProjectStatus? status = null)
        {
            try
            {
                var query = new GetProjectsByCursorQuery { Cursor = cursor, PageSize = pageSize, Status = status };
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = ex.Message,
                    details = Array.Empty<object>()
                });
            }
        }
    }
}
