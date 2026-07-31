using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Http;

/// <summary>
/// The identifiers live outside the HTTP handler because the handler chain is recycled periodically
/// and a handler-local value would silently change mid-session.
/// </summary>
internal sealed class ClientIdentity(IJSRuntime jsRuntime) : IClientIdentity
{
    private const string MachineIdKey = "evilcase.machine-id";

    public string MachineId { get; } = ReadOrCreateMachineId(jsRuntime as IJSInProcessRuntime);

    public string SessionId { get; } = NewId();

    private static string NewId() => Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);

    /// <summary>
    /// WebAssembly allows synchronous interop, which is what makes the identifier available to the
    /// handler; a runtime without it falls back to an identifier for this load only.
    /// </summary>
    private static string ReadOrCreateMachineId(IJSInProcessRuntime? runtime)
    {
        if (runtime is null)
            return NewId();

        if (Guid.TryParse(runtime.Invoke<string?>("localStorage.getItem", MachineIdKey), out var stored))
            return stored.ToString("D", CultureInfo.InvariantCulture);

        var created = NewId();
        runtime.InvokeVoid("localStorage.setItem", MachineIdKey, created);

        return created;
    }
}
