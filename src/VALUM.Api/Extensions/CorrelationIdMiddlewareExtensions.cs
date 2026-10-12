using Microsoft.AspNetCore.Builder;

namespace VALUM.Api.Extensions
{
    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<VALUM.Api.Middleware.CorrelationIdMiddleware>();
        }
    }
}
