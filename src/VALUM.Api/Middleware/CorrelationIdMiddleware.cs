using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace VALUM.Api.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CorrelationIdHeaderName = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string? correlationId = null;

            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdValues))
            {
                var incomingId = correlationIdValues.FirstOrDefault();
                
                // Validate GUID format - only accept valid GUIDs
                if (incomingId != null && Guid.TryParse(incomingId, out var parsedGuid) && parsedGuid != Guid.Empty)
                {
                    correlationId = incomingId;
                }
            }

            // Generate new GUID if missing, empty, or invalid
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            // Add to Serilog LogContext for structured logging with proper scope disposal
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                // Add to response header
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
                    return Task.CompletedTask;
                });

                await _next(context);
            }
        }
    }
}
