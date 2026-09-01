using System.Text;
using EvilBrains.EvilCase.Api.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EvilBrains.EvilCase.Tests.HealthChecks;

public class HealthCheckResponseWriterTests
{
    [Test]
    public async Task OnlyNamesAndStatusesAreWritten()
    {
        var report = Report(
            ("database", Entry(HealthStatus.Healthy)),
            ("queue", Entry(HealthStatus.Degraded)));

        var json = await Write(report);

        Assert.That(
            json,
            Is.EqualTo("""{"status":"Degraded","checks":[{"name":"database","status":"Healthy"},{"name":"queue","status":"Degraded"}]}"""));
    }

    [Test]
    public async Task DescriptionsExceptionsAndDataStayOut()
    {
        var entry = new HealthReportEntry(
            HealthStatus.Unhealthy,
            "Cannot reach db.internal:5432",
            TimeSpan.FromSeconds(1),
            new InvalidOperationException("password authentication failed for user evilcase"),
            new Dictionary<string, object>(StringComparer.Ordinal) { ["connectionString"] = "Host=db.internal;Password=hunter2" });

        var json = await Write(Report(("database", entry)));

        Assert.That(json, Is.EqualTo("""{"status":"Unhealthy","checks":[{"name":"database","status":"Unhealthy"}]}"""));
    }

    private static HealthReportEntry Entry(HealthStatus status)
    {
        return new(status, description: null, TimeSpan.Zero, exception: null, data: null);
    }

    private static HealthReport Report(params (string Name, HealthReportEntry Entry)[] entries)
    {
        return new(entries.ToDictionary(static x => x.Name, static x => x.Entry, StringComparer.Ordinal), TimeSpan.FromSeconds(1));
    }

    private static async Task<string> Write(HealthReport report)
    {
        var context = new DefaultHttpContext();
        await using var body = new MemoryStream();
        context.Response.Body = body;

        await HealthCheckResponseWriter.Write(context, report);

        return Encoding.UTF8.GetString(body.ToArray());
    }
}
