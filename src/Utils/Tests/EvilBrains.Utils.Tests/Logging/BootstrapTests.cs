using EvilBrains.Logging.WebAssembly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace EvilBrains.Utils.Tests.Logging;

public class BootstrapTests
{
    [Test]
    public void ConsoleWritesThroughTheHostRuntime()
    {
        var runtime = new RecordingJSRuntime();
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(runtime);

        using var logger = Bootstrap.CreateLogger(services, new ClientLoggingOptions(), new ClientLogSink());
        logger.Information("Message");

        Assert.That(runtime.Identifiers, Is.EqualTo(["console.info"]), "a runtime of the sink's own completes the host's calls of the same number");
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
