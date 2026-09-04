namespace EvilBrains.Logging.WebAssembly;

internal interface IClientIdentity
{
    public string MachineId { get; }

    public string SessionId { get; }
}
