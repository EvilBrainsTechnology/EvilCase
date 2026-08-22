using Microsoft.AspNetCore.Builder;

namespace EvilBrains.Logging.AspNetCore;

public static class RequestContextLoggingApplicationBuilderExtensions
{
    /// <summary>
    /// Must run before the Serilog request logging middleware so the request completion event
    /// carries the identifiers as well.
    /// </summary>
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestContextLoggingMiddleware>();
    }
}
