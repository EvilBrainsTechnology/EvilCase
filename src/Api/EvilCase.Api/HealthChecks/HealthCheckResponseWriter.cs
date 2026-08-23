using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EvilBrains.EvilCase.Api.HealthChecks;

/// <summary>
/// Writes the check names and their status. Descriptions, exceptions and data stay out:
/// the endpoint is anonymous.
/// </summary>
internal static class HealthCheckResponseWriter
{
    public static Task Write(HttpContext context, HealthReport report)
    {
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new { name = entry.Key, status = entry.Value.Status.ToString() }),
        };

        return context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }
}
