using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.Logging.AspNetCore;
using EvilBrains.Logging.Contract;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class LogsControllerTests
{
    [Test]
    public void EveryEntryIsForwardedToTheWriter()
    {
        var writer = new CollectingWriter();
        var controller = new LogsController();

        controller.WriteClientLogs(writer, new ClientLogBatch { Entries = [Entry("first"), Entry("second")] });

        Assert.That(writer.Entries.Select(static x => x.MessageTemplate), Is.EqualTo(["first", "second"]));
    }

    /// <summary>
    /// The endpoint is anonymous and model validation covers properties, never collection elements.
    /// </summary>
    [Test]
    public void NullEntryIsSkipped()
    {
        var writer = new CollectingWriter();
        var controller = new LogsController();

        controller.WriteClientLogs(writer, new ClientLogBatch { Entries = [null!, Entry("second")] });

        Assert.That(writer.Entries.Select(static x => x.MessageTemplate), Is.EqualTo(["second"]));
    }

    private static ClientLogEntry Entry(string messageTemplate)
    {
        return new()
        {
            Timestamp = DateTimeOffset.Now,
            Level = ClientLogLevel.Warning,
            MessageTemplate = messageTemplate,
        };
    }

    private sealed class CollectingWriter : IClientLogWriter
    {
        public List<ClientLogEntry> Entries { get; } = [];

        public void Write(ClientLogEntry entry)
        {
            this.Entries.Add(entry);
        }
    }
}
