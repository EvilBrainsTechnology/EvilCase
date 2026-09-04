using System.Text.Json;
using Microsoft.JSInterop;
using Serilog.Debugging;

namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// The periodic loop drains once a second; an error logged just before a reload would otherwise die with the runtime.
/// </summary>
internal sealed class PageUnloadFlusher(IJSRuntime jsRuntime, ClientLogSink sink, string uploadUrl) : IPageUnloadFlusher, IAsyncDisposable
{
    private const string ModulePath = "./_content/EvilBrains.Logging.WebAssembly/client-log-flush.js";

    private DotNetObjectReference<PageUnloadFlusher>? reference;

    private IJSObjectReference? module;

    /// <summary>
    /// Called from the unload handler, which cannot await: the entries are handed over as the request
    /// body for a beacon rather than uploaded from here.
    /// </summary>
    [JSInvokable]
    public string? Flush()
    {
        var batch = sink.Drain();

        return batch is null ? null : JsonSerializer.Serialize(batch, ClientLogJsonContext.Default.ClientLogBatch);
    }

    public async Task Start()
    {
        try
        {
            this.reference = DotNetObjectReference.Create(this);
            this.module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);

            await this.module.InvokeVoidAsync("register", uploadUrl, this.reference);
        }
        catch (Exception exception)
        {
            // Broad on purpose: nothing observes the task, so anything narrower would surface as an
            // unobserved task exception instead of this line.
            SelfLog.WriteLine("Flushing log entries on page unload is unavailable: {0}", exception);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
        {
            try
            {
                await this.module.InvokeVoidAsync("unregister");
                await this.module.DisposeAsync();
            }
            catch (JSException exception)
            {
                SelfLog.WriteLine("Releasing the page unload handler failed: {0}", exception);
            }
        }

        this.reference?.Dispose();
    }
}
