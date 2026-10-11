using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using VALUM.Application.Exceptions;
using VALUM.Domain.Exceptions;

namespace VALUM.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            int status;
            string errorType;
            string message = exception.Message ?? "An error occurred.";
            object[] details = Array.Empty<object>();

            if (exception is VALUM.Application.Exceptions.ValidationException)
            {
                status = StatusCodes.Status400BadRequest;
                errorType = "ValidationException";
                // Use ValidationException.Errors if available
                var vex = exception as VALUM.Application.Exceptions.ValidationException;
                details = vex?.Errors?.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }).ToArray() ?? Array.Empty<object>();
            }
            else if (exception is NotFoundException)
            {
                status = StatusCodes.Status404NotFound;
                errorType = "NotFoundException";
            }
            else if (exception is DomainException)
            {
                status = StatusCodes.Status422UnprocessableEntity;
                errorType = "DomainException";
            }
            else
            {
                status = StatusCodes.Status500InternalServerError;
                errorType = "Exception";
                // Do not include exception details for 500
                message = "An unexpected error occurred.";
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
