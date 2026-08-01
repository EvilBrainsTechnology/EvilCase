namespace EvilBrains.Logging.WebAssembly;

internal interface IPageUnloadFlusher
{
    /// <summary>
    /// Hooks the page unload event. Nothing awaits the result.
    /// </summary>
    public Task Start();
}
