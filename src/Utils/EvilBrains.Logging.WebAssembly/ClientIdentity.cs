using Microsoft.JSInterop;

namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// The identifiers live outside the HTTP handler because the handler chain is recycled periodically
/// and a handler-local value would silently change mid-session.
/// </summary>
internal sealed class ClientIdentity(IJSRuntime jsRuntime, string machineIdStorageKey) : IClientIdentity
{
    public string MachineId { get; } = ReadOrCreateMachineId(jsRuntime as IJSInProcessRuntime, machineIdStorageKey);

    public string SessionId { get; } = NewId();

    private static string NewId() => Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);

    /// <summary>
    /// WebAssembly allows synchronous interop, which is what makes the identifier available to the
    /// handler; a runtime without it falls back to an identifier for this load only.
    /// </summary>
    private static string ReadOrCreateMachineId(IJSInProcessRuntime? runtime, string storageKey)
    {
        if (runtime is null)
            return NewId();

        if (Guid.TryParse(runtime.Invoke<string?>("localStorage.getItem", storageKey), out var stored))
            return stored.ToString("D", CultureInfo.InvariantCulture);

        var created = NewId();
        runtime.InvokeVoid("localStorage.setItem", storageKey, created);

        return created;
    }
}
