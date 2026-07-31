using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EvilBrains.EvilCase.Api.HealthChecks;

internal static class HealthCheckResponseWriter
{
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        await using var writer = new Utf8JsonWriter(context.Response.BodyWriter);

        writer.WriteStartObject();
        writer.WriteString("status", report.Status.ToString());
        writer.WriteNumber("durationMs", report.TotalDuration.TotalMilliseconds);
        writer.WriteStartObject("checks");

        foreach (var (name, entry) in report.Entries)
        {
            writer.WriteStartObject(name);
            writer.WriteString("status", entry.Status.ToString());
            writer.WriteNumber("durationMs", entry.Duration.TotalMilliseconds);

            if (entry.Description is not null)
                writer.WriteString("description", entry.Description);

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();

        await writer.FlushAsync(context.RequestAborted);
    }
}
