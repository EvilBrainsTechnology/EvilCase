using System.Net;
using EvilBrains.Logging.WebAssembly;
using Microsoft.Extensions.Logging;

namespace EvilBrains.Utils.Tests.Logging;

public class ClientHttpLoggerTests
{
    private const string UploadPath = "/api/logs/client";

    [Test]
    public void SuccessfulUploadOfClientLogsIsNotLogged()
    {
        Assert.That(LoggedPaths("https://localhost/api/logs/client"), Is.Empty);
    }

    [Test]
    public void SuccessfulUploadOfClientLogsIsNotLoggedUnderASubPath()
    {
        Assert.That(LoggedPaths("https://localhost/evilcase/api/logs/client"), Is.Empty, "an equality match on the path would let a sub-path upload log itself");
    }

    [Test]
    public void RejectedUploadOfClientLogsIsLogged()
    {
        string[] expected = ["/api/logs/client"];

        Assert.That(LoggedPaths("https://localhost/api/logs/client", HttpStatusCode.BadRequest), Is.EqualTo(expected), "a rejected batch is dropped, so logging it settles");
    }

    [Test]
    public void AnyOtherRequestIsLogged()
    {
        string[] expected = ["/evilcase/api/echo/post"];

        Assert.That(LoggedPaths("https://localhost/evilcase/api/echo/post"), Is.EqualTo(expected));
    }

    [Test]
    public void PathMatchingIsOnSegmentBoundaries()
    {
        string[] expected = ["/xapi/logs/client"];

        Assert.That(LoggedPaths("https://localhost/xapi/logs/client"), Is.EqualTo(expected));
    }

    [Test]
    public void FailedUploadOfClientLogsIsLogged()
    {
        var logger = new CollectingLogger();
        var subject = new ClientHttpLogger(logger, UploadPath);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://localhost/api/logs/client"));

        subject.LogRequestFailed(context: null, request, response: null, new HttpRequestException(), TimeSpan.Zero);

        Assert.That(logger.Paths, Has.Count.EqualTo(1), "a failed batch is dropped, so logging it settles");
    }

    private static List<string> LoggedPaths(string url, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var logger = new CollectingLogger();
        var subject = new ClientHttpLogger(logger, UploadPath);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
        using var response = new HttpResponseMessage(statusCode);

        subject.LogRequestStop(context: null, request, response, TimeSpan.Zero);

        return logger.Paths;
    }

    private sealed class CollectingLogger : ILogger<ClientHttpLogger>, IDisposable
    {
        public List<string> Paths { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return this;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var properties = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
            var path = properties.FirstOrDefault(static x => string.Equals(x.Key, "RequestPath", StringComparison.Ordinal)).Value;

            this.Paths.Add(path?.ToString() ?? "");
        }

        public void Dispose() { }
    }
}
