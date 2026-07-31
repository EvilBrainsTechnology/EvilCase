using EvilBrains.EvilCase.Api.Logging;
using EvilBrains.Logging.Contract;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace EvilBrains.EvilCase.Tests.Logging;

public class ClientLogWriterTests
{
    private CollectingSink sink = null!;

    private ClientLogWriter writer = null!;

    [SetUp]
    public void SetUp()
    {
        this.sink = new CollectingSink();

        var logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(this.sink).CreateLogger();
        this.writer = new ClientLogWriter(logger);
    }

    [Test]
    public void ReferencedPropertyIsBound()
    {
        this.writer.Write(Entry("Case {CaseId} not found", new Dictionary<string, string>(StringComparer.Ordinal) { ["CaseId"] = "42" }));

        var logEvent = this.sink.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Value(logEvent, "CaseId"), Is.EqualTo("42"));
            Assert.That(logEvent.RenderMessage(CultureInfo.InvariantCulture), Does.Contain("42"));
        }
    }

    [Test]
    public void UnreferencedPropertyIsDropped()
    {
        this.writer.Write(Entry("Nothing to bind", new Dictionary<string, string>(StringComparer.Ordinal) { ["CaseId"] = "42" }));

        Assert.That(Value(this.sink.Single(), "CaseId"), Is.Null);
    }

    [Test]
    public void ReservedPropertyIsNotBound()
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal) { ["RequestId"] = "spoofed", ["SourceContext"] = "spoofed" };

        this.writer.Write(Entry("{RequestId} {SourceContext}", properties));

        var logEvent = this.sink.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Value(logEvent, "RequestId"), Is.Null);
            Assert.That(Value(logEvent, "SourceContext"), Is.EqualTo("EvilBrains.EvilCase.App.Client"));
        }
    }

    [Test]
    public void OverflowingAlignmentDoesNotThrow()
    {
        this.writer.Write(Entry("{Foo,-2147483648}", new Dictionary<string, string>(StringComparer.Ordinal) { ["Foo"] = "bar" }));

        Assert.That(this.sink.Single().RenderMessage(CultureInfo.InvariantCulture), Does.Contain("Foo"));
    }

    [Test]
    public void WideAlignmentIsNotBound()
    {
        this.writer.Write(Entry("{Foo,100000}", new Dictionary<string, string>(StringComparer.Ordinal) { ["Foo"] = "bar" }));

        var logEvent = this.sink.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Value(logEvent, "Foo"), Is.Null);
            Assert.That(logEvent.RenderMessage(CultureInfo.InvariantCulture), Has.Length.LessThan(100));
        }
    }

    [Test]
    public void ControlCharactersAreStripped()
    {
        this.writer.Write(Entry("{Foo}", new Dictionary<string, string>(StringComparer.Ordinal) { ["Foo"] = "first\n[ERR] forged" }));

        Assert.That(Value(this.sink.Single(), "Foo"), Is.EqualTo("first[ERR] forged"));
    }

    [Test]
    public void PropertyCountIsClamped()
    {
        var names = Enumerable.Range(0, ClientLogEntry.MaxProperties + 4).Select(x => "P" + x.ToString(CultureInfo.InvariantCulture)).ToList();
        var template = string.Concat(names.Select(x => "{" + x + "}"));
        var properties = names.ToDictionary(x => x, _ => "value", StringComparer.Ordinal);

        this.writer.Write(Entry(template, properties));

        Assert.That(this.sink.Single().Properties.Keys.Count(x => x.StartsWith('P')), Is.EqualTo(ClientLogEntry.MaxProperties));
    }

    [Test]
    public void EventUsesServerClock()
    {
        var clientTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

        this.writer.Write(Entry("Stale clock", properties: null) with { Timestamp = clientTime });

        var logEvent = this.sink.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(logEvent.Timestamp.Year, Is.GreaterThan(2000));
            Assert.That(logEvent.Properties["ClientTimestamp"].ToString(format: null, CultureInfo.InvariantCulture), Does.Contain("2000"));
        }
    }

    private static ClientLogEntry Entry(string messageTemplate, IReadOnlyDictionary<string, string>? properties) => new()
    {
        Timestamp = DateTimeOffset.Now,
        Level = ClientLogLevel.Warning,
        MessageTemplate = messageTemplate,
        Properties = properties,
    };

    private static string? Value(LogEvent logEvent, string name) =>
        logEvent.Properties.TryGetValue(name, out var value) && value is ScalarValue { Value: string text } ? text : null;

    private sealed class CollectingSink : ILogEventSink
    {
        private readonly List<LogEvent> events = [];

        public void Emit(LogEvent logEvent) => this.events.Add(logEvent);

        public LogEvent Single() => this.events.Single();
    }
}
