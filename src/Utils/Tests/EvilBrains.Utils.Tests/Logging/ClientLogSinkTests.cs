using System.Text.Json;
using EvilBrains.Logging.Contract;
using EvilBrains.Logging.WebAssembly;
using Serilog.Events;
using Serilog.Parsing;

namespace EvilBrains.Utils.Tests.Logging;

public class ClientLogSinkTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Test]
    public void EmptyBufferDrainsToNothing() => Assert.That(new ClientLogSink().Drain(), Is.Null);

    [Test]
    public void BufferedEventsDrainInBatches()
    {
        var sink = new ClientLogSink();
        for (var index = 0; index < ClientLogBatch.MaxEntries + 1; index++)
            sink.Emit(Event("Message"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sink.Drain()?.Entries, Has.Count.EqualTo(ClientLogBatch.MaxEntries));
            Assert.That(sink.Drain()?.Entries, Has.Count.EqualTo(1));
            Assert.That(sink.Drain(), Is.Null);
        }
    }

    /// <summary>
    /// A cut between a high and a low surrogate leaves a lone surrogate, which the JSON writer rejects
    /// with an exception the uploader does not translate — the whole batch would be lost.
    /// </summary>
    [Test]
    public void TruncatedTextStaysSerializable()
    {
        var sink = new ClientLogSink();
        sink.Emit(Event(new string('a', ClientLogEntry.MessageTemplateMaxLength - 1) + "😀"));

        var batch = sink.Drain();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(batch?.Entries[0].MessageTemplate, Has.Length.EqualTo(ClientLogEntry.MessageTemplateMaxLength - 1));
            Assert.That(() => JsonSerializer.Serialize(batch, JsonOptions), Throws.Nothing);
        }
    }

    private static LogEvent Event(string messageTemplate) => new(
        DateTimeOffset.Now,
        LogEventLevel.Warning,
        exception: null,
        new MessageTemplateParser().Parse(messageTemplate),
        []);
}
