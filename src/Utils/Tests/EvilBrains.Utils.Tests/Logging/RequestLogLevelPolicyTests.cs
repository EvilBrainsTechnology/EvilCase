using EvilBrains.Logging.AspNetCore;
using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace EvilBrains.Utils.Tests.Logging;

public class RequestLogLevelPolicyTests
{
    private static readonly string[] LoggedPaths = ["/api"];

    private static readonly string[] QuietPaths = ["/api/logs/client"];

    [Test]
    public void RequestUnderALoggedPathIsLogged() =>
        Assert.That(Level("/api/echo/post"), Is.EqualTo(LogEventLevel.Information));

    [Test]
    public void RequestOutsideTheLoggedPathsLeavesNoLog()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Level("/"), Is.EqualTo(LogEventLevel.Verbose), "the frontend itself");
            Assert.That(Level("/_framework/blazor.webassembly.js"), Is.EqualTo(LogEventLevel.Verbose), "a static asset");
            Assert.That(Level("/health/ready"), Is.EqualTo(LogEventLevel.Verbose), "a health probe");
        }
    }

    [Test]
    public void PathMatchingIsOnSegmentBoundaries() =>
        Assert.That(Level("/apiary/items"), Is.EqualTo(LogEventLevel.Verbose));

    [Test]
    public void PreflightIsNeverLogged() =>
        Assert.That(Level("/api/echo/post", method: HttpMethods.Options), Is.EqualTo(LogEventLevel.Verbose));

    /// <summary>
    /// Logging a successful upload would ship that log with the next upload, which would log again.
    /// </summary>
    [Test]
    public void SuccessfulUploadOfClientLogsLeavesNoLog() =>
        Assert.That(Level("/api/logs/client"), Is.EqualTo(LogEventLevel.Verbose));

    /// <summary>
    /// A rejected batch is dropped rather than retried, so logging it settles instead of feeding itself.
    /// </summary>
    [Test]
    public void RejectedUploadOfClientLogsIsLogged() =>
        Assert.That(Level("/api/logs/client", statusCode: StatusCodes.Status400BadRequest), Is.EqualTo(LogEventLevel.Information));

    [Test]
    public void ServerErrorIsLoggedWhereverItHappens()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Level("/_framework/blazor.webassembly.js", StatusCodes.Status500InternalServerError), Is.EqualTo(LogEventLevel.Error));
            Assert.That(Level("/api/logs/client", StatusCodes.Status503ServiceUnavailable), Is.EqualTo(LogEventLevel.Error));
        }
    }

    [Test]
    public void UnhandledExceptionIsLoggedWhereverItHappens() =>
        Assert.That(Level("/", exception: new InvalidOperationException()), Is.EqualTo(LogEventLevel.Error));

    private static LogEventLevel Level(
        string path,
        int statusCode = StatusCodes.Status200OK,
        string? method = null,
        Exception? exception = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method ?? HttpMethods.Get;
        context.Request.Path = path;
        context.Response.StatusCode = statusCode;

        var policy = new RequestLogLevelPolicy(LoggedPaths, QuietPaths);

        return policy.GetLevel(context, 0, exception);
    }
}
