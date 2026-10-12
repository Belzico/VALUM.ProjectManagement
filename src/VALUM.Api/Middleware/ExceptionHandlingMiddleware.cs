using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VALUM.Application.Exceptions;
using VALUM.Domain.Exceptions;

namespace VALUM.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            int status;
            string errorType;
            string message = exception.Message ?? "An error occurred.";
            object[] details = Array.Empty<object>();

            if (exception is VALUM.Application.Exceptions.ValidationException vex)
            {
                status = StatusCodes.Status400BadRequest;
                errorType = "ValidationException";
                details = vex?.Errors?.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }).ToArray() ?? Array.Empty<object>();

                var failedFields = vex?.Errors?.Select(e => e.PropertyName).Distinct().ToArray() ?? Array.Empty<string>();
                _logger.LogWarning(
                    "Validation failed. Failed fields: {FailedFields}. Failures: {FailureCount}",
                    failedFields,
                    vex?.Errors?.Count ?? 0);
            }
            else if (exception is NotFoundException)
            {
                status = StatusCodes.Status404NotFound;
                errorType = "NotFoundException";
                _logger.LogWarning("Resource not found: {ResourceType}", exception.Message);
            }
            else if (exception is DomainException dex)
            {
                status = StatusCodes.Status422UnprocessableEntity;
                errorType = "DomainException";
                _logger.LogWarning("Business rule violation: {Rule}", exception.Message);
            }
            else
            {
                status = StatusCodes.Status500InternalServerError;
                errorType = "Exception";
                message = "An unexpected error occurred.";
                _logger.LogError(exception, "Unexpected error occurred");
            }

            var payload = new
            {
                status,
                error = errorType,
                message,
                details
            };

            var json = JsonSerializer.Serialize(payload);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = status;
            return context.Response.WriteAsync(json);
        }
    }
}
