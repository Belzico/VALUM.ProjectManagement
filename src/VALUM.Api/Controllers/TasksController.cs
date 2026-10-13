using System;
using System.Globalization;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VALUM.Application.Tasks.Commands;
using VALUM.Application.Tasks.DTOs;
using VALUM.Application.Tasks.Queries;
using VALUM.Domain.Enums;

namespace VALUM.Api.Controllers
{
    public class CreateTaskRequest
    {
        public string title { get; set; } = string.Empty;
        public string? description { get; set; }
        public int priority { get; set; }
        public string? dueDate { get; set; }
    }

    public class UpdateTaskStatusRequest
    {
        public int status { get; set; }
    }

    [ApiController]
    [Route("api/projects/{projectId}/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TasksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromRoute] string projectId, [FromBody] CreateTaskRequest request)
        {
            if (!Guid.TryParse(projectId, out var guidProjectId))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Invalid project ID format.",
                    details = Array.Empty<object>()
                });
            }

            // Validate priority is within enum range
            if (request.priority < 0 || request.priority > 2)
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Priority must be a valid TaskPriority value (0=Low, 1=Medium, 2=High).",
                    details = Array.Empty<object>()
                });
            }

            DateTime? dueDateUtc = null;
            if (request.dueDate != null)
            {
                // Reject empty or whitespace-only strings (not valid ISO 8601)
                if (string.IsNullOrWhiteSpace(request.dueDate))
                {
                    return BadRequest(new
                    {
                        status = 400,
                        error = "ValidationException",
                        message = "DueDate must be a valid ISO 8601 date-time with 'T' separator and timezone (Z or ±HH:mm).",
                        details = Array.Empty<object>()
                    });
                }

                // Normalize trailing Z to +00:00 for explicit UTC offset parsing
                var normalizedInput = request.dueDate.EndsWith("Z") ? request.dueDate.Substring(0, request.dueDate.Length - 1) + "+00:00" : request.dueDate;

                // Strict ISO 8601 parsing with culture-invariant formats
                // Formats with explicit offset (zzz) ensure Z is interpreted as UTC, not local time
                // Support 0-7 fractional digits
                var formats = new[]
                {
                    "yyyy-MM-ddTHH:mm:sszzz",           // No fraction
                    "yyyy-MM-ddTHH:mm:ss.fzzz",          // 1 digit
                    "yyyy-MM-ddTHH:mm:ss.ffzzz",         // 2 digits
                    "yyyy-MM-ddTHH:mm:ss.fffzzz",        // 3 digits
                    "yyyy-MM-ddTHH:mm:ss.ffffzzz",       // 4 digits
                    "yyyy-MM-ddTHH:mm:ss.fffffzzz",      // 5 digits
                    "yyyy-MM-ddTHH:mm:ss.ffffffzzz",     // 6 digits
                    "yyyy-MM-ddTHH:mm:ss.fffffffzzz"     // 7 digits
                };

                if (!DateTimeOffset.TryParseExact(normalizedInput, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dueDateOffset))
                {
                    return BadRequest(new
                    {
                        status = 400,
                        error = "ValidationException",
                        message = "DueDate must be a valid ISO 8601 date-time with 'T' separator and timezone (Z or ±HH:mm).",
                        details = Array.Empty<object>()
                    });
                }

                dueDateUtc = dueDateOffset.UtcDateTime;
            }

            var command = new CreateTaskCommand
            {
                ProjectId = guidProjectId,
                Title = request.title,
                Description = request.description,
                Priority = (TaskPriority)request.priority,
                DueDate = dueDateUtc
            };

            var dto = await _mediator.Send(command);
            return Created($"/api/tasks/{dto.Id}", dto);
        }

        [HttpGet("")]
        public async Task<IActionResult> Get([FromRoute] string projectId, [FromQuery] VALUM.Domain.Enums.TaskStatus? status, [FromQuery] TaskPriority? priority, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (!Guid.TryParse(projectId, out var guidProjectId))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Invalid project ID format.",
                    details = Array.Empty<object>()
                });
            }

            // Validate status is within enum range if provided
            if (status.HasValue && ((int)status.Value < 0 || (int)status.Value > 2))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Status must be a valid TaskStatus value (0=Todo, 1=InProgress, 2=Done).",
                    details = Array.Empty<object>()
                });
            }

            // Validate priority is within enum range if provided
            if (priority.HasValue && ((int)priority.Value < 0 || (int)priority.Value > 2))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Priority must be a valid TaskPriority value (0=Low, 1=Medium, 2=High).",
                    details = Array.Empty<object>()
                });
            }

            var query = new GetTasksQuery { ProjectId = guidProjectId, Status = status, Priority = priority, Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("cursor")]
        public async Task<IActionResult> GetByCursor([FromRoute] string projectId, [FromQuery] string? cursor, [FromQuery] int pageSize = 10, [FromQuery] VALUM.Domain.Enums.TaskStatus? status = null, [FromQuery] TaskPriority? priority = null)
        {
            if (!Guid.TryParse(projectId, out var guidProjectId))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Invalid project ID format.",
                    details = Array.Empty<object>()
                });
            }

            try
            {
                var query = new GetTasksByCursorQuery { ProjectId = guidProjectId, Cursor = cursor, PageSize = pageSize, Status = status, Priority = priority };
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

    [ApiController]
    [Route("api/tasks")]
    public class TaskDetailController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TaskDetailController(IMediator mediator)
        {
            _mediator = mediator;
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
                    message = "Invalid task ID format.",
                    details = Array.Empty<object>()
                });
            }

            var query = new GetTaskByIdQuery { Id = guidId };
            var dto = await _mediator.Send(query);
            return Ok(dto);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus([FromRoute] string id, [FromBody] UpdateTaskStatusRequest request)
        {
            if (!Guid.TryParse(id, out var guidId))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Invalid task ID format.",
                    details = Array.Empty<object>()
                });
            }

            // Validate status is within enum range
            if (request.status < 0 || request.status > 2)
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Status must be a valid TaskStatus value (0=Todo, 1=InProgress, 2=Done).",
                    details = Array.Empty<object>()
                });
            }

            var command = new UpdateTaskStatusCommand { Id = guidId, Status = (VALUM.Domain.Enums.TaskStatus)request.status };
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            if (!Guid.TryParse(id, out var guidId))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "ValidationException",
                    message = "Invalid task ID format.",
                    details = Array.Empty<object>()
                });
            }

            var command = new DeleteTaskCommand { Id = guidId };
            await _mediator.Send(command);
            return NoContent();
        }
    }
}
