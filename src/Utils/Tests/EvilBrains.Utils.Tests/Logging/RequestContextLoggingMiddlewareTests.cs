using EvilBrains.Logging.AspNetCore;
using EvilBrains.Logging.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace EvilBrains.Utils.Tests.Logging;

public class RequestContextLoggingMiddlewareTests
{
    [Test]
    public async Task EveryEventOfTheRequestCarriesAllFourIdentifiers()
    {
        var context = Request();
        var requestId = Set(context, RequestContextHeaderNames.RequestId);
        var correlationId = Set(context, RequestContextHeaderNames.CorrelationId);
        var sessionId = Set(context, RequestContextHeaderNames.SessionId);
        var machineId = Set(context, RequestContextHeaderNames.MachineId);

        var events = await LogDuringRequest(context, "First", "Second");

        using (Assert.EnterMultipleScope())
        {
            foreach (var logEvent in events)
            {
                Assert.That(Value(logEvent, RequestContextPropertyNames.RequestId), Is.EqualTo(requestId));
                Assert.That(Value(logEvent, RequestContextPropertyNames.CorrelationId), Is.EqualTo(correlationId));
                Assert.That(Value(logEvent, RequestContextPropertyNames.SessionId), Is.EqualTo(sessionId));
                Assert.That(Value(logEvent, RequestContextPropertyNames.MachineId), Is.EqualTo(machineId));
            }
        }
    }

    /// <summary>
    /// ASP.NET Core opens a scope per request whose RequestId is the trace identifier, and a scope
    /// property reaches the event ahead of the log context. The caller's identifiers sit next to it
    /// under their own names instead of competing with it.
    /// </summary>
    [Test]
    public async Task HostingScopeKeepsRequestIdAndLeavesTheCallerIdentifiersAlone()
    {
        var context = Request();
        context.TraceIdentifier = "0HN7TRACE:00000001";
        var requestId = Set(context, RequestContextHeaderNames.RequestId);

        var sink = new CollectingSink();
        using var factory = new SerilogLoggerFactory(Logger(sink));
        var logger = factory.CreateLogger("Test");

        var middleware = new RequestContextLoggingMiddleware(async _ =>
        {
            using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = context.TraceIdentifier }))
                logger.LogInformation("Inside the hosting scope");
        });

        await middleware.Invoke(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Value(sink.Events.Single(), RequestContextPropertyNames.RequestId), Is.EqualTo(requestId));
            Assert.That(Value(sink.Events.Single(), "RequestId"), Is.EqualTo("0HN7TRACE:00000001"));
        }
    }

    /// <summary>
    /// The hosting scope only reaches what is logged through ILogger&lt;T&gt;; Serilog writes its own
    /// request completion event outside it, and that one needs the trace identifier too.
    /// </summary>
    [Test]
    public async Task TraceIdentifierIsOnEventsWrittenOutsideTheHostingScope()
    {
        var context = Request();
        context.TraceIdentifier = "0HN7TRACE:00000002";

        var logEvent = (await LogDuringRequest(context, "Inside")).Single();

        Assert.That(Value(logEvent, "RequestId"), Is.EqualTo("0HN7TRACE:00000002"));
    }

    [Test]
    public async Task IdentifiersAreGoneOnceTheRequestEnded()
    {
        var context = Request();
        Set(context, RequestContextHeaderNames.RequestId);
        Set(context, RequestContextHeaderNames.SessionId);

        await LogDuringRequest(context, "Inside");

        var sink = new CollectingSink();
        Logger(sink).Information("Outside");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Value(sink.Events.Single(), RequestContextPropertyNames.RequestId), Is.Null);
            Assert.That(Value(sink.Events.Single(), RequestContextPropertyNames.SessionId), Is.Null);
        }
    }

    [Test]
    public async Task RequestWithoutIdentifiersCarriesTheTraceIdentifierOnly()
    {
        var context = Request();
        context.TraceIdentifier = "0HN7TRACE:00000001";

        var logEvent = (await LogDuringRequest(context, "Inside")).Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Value(logEvent, "RequestId"), Is.EqualTo("0HN7TRACE:00000001"));
            Assert.That(Value(logEvent, RequestContextPropertyNames.RequestId), Is.Null);
            Assert.That(Value(logEvent, RequestContextPropertyNames.CorrelationId), Is.Null);
            Assert.That(Value(logEvent, RequestContextPropertyNames.SessionId), Is.Null);
            Assert.That(Value(logEvent, RequestContextPropertyNames.MachineId), Is.Null);
        }
    }

    [Test]
    public async Task MalformedAndRepeatedHeadersAreRejected()
    {
        var context = Request();
        context.Request.Headers[RequestContextHeaderNames.SessionId] = "not a guid";
        context.Request.Headers[RequestContextHeaderNames.MachineId] = new string[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

        var logEvent = (await LogDuringRequest(context, "Inside")).Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Value(logEvent, RequestContextPropertyNames.SessionId), Is.Null);
            Assert.That(Value(logEvent, RequestContextPropertyNames.MachineId), Is.Null);
        }
    }

    private static DefaultHttpContext Request()
    {
        return new();
    }

    private static string Set(HttpContext context, string headerName)
    {
        var id = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
        context.Request.Headers[headerName] = id;

        return id;
    }

    private static Logger Logger(CollectingSink sink)
    {
        return new LoggerConfiguration().MinimumLevel.Verbose().Enrich.FromLogContext().WriteTo.Sink(sink).CreateLogger();
    }

    private static async Task<IReadOnlyList<LogEvent>> LogDuringRequest(HttpContext context, params string[] messages)
    {
        var sink = new CollectingSink();
        var logger = Logger(sink);

        // Logging from a continuation as well: a handler that awaits must not lose the identifiers.
        var middleware = new RequestContextLoggingMiddleware(async _ =>
        {
            foreach (var message in messages)
            {
                logger.Information("{Message}", message);
                await Task.Yield();
            }
        });

        await middleware.Invoke(context);

        return sink.Events;
    }

    private static string? Value(LogEvent logEvent, string name)
    {
        return logEvent.Properties.TryGetValue(name, out var value) && value is ScalarValue { Value: string text } ? text : null;
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent)
        {
            this.Events.Add(logEvent);
        }
    }
}
