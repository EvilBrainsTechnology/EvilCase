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
        var controller = new LogsController(writer);

        controller.WriteClientLogs(new ClientLogBatch { Entries = [Entry("first"), Entry("second")] });

        Assert.That(writer.Entries.Select(x => x.MessageTemplate), Is.EqualTo(["first", "second"]));
    }

    private static ClientLogEntry Entry(string messageTemplate) => new()
    {
        Timestamp = DateTimeOffset.Now,
        Level = ClientLogLevel.Warning,
        MessageTemplate = messageTemplate,
    };

    private sealed class CollectingWriter : IClientLogWriter
    {
        public List<ClientLogEntry> Entries { get; } = [];

        public void Write(ClientLogEntry entry) => this.Entries.Add(entry);
    }
}
