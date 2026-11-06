using EvilBrains.Logging.WebAssembly;
using Microsoft.JSInterop;

namespace EvilBrains.Utils.Tests.Logging;

public class BootstrapTests
{
    [Test]
    public void ConsoleWritesThroughTheHostRuntime()
    {
        var runtime = new RecordingJSRuntime();

        using var logger = Bootstrap.CreateLogger(runtime, new ClientLoggingOptions(), new ClientLogSink());
        logger.Information("Message");

        Assert.That(runtime.Identifiers, Is.EqualTo(["console.info"]), "the console writes through the runtime it is given, never one of its own");
    }

    private sealed class RecordingJSRuntime : IJSRuntime
    {
        public List<string> Identifiers { get; } = [];

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return await this.InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            this.Identifiers.Add(identifier);

            return ValueTask.FromResult(default(TValue)!);
        }
    }
}
