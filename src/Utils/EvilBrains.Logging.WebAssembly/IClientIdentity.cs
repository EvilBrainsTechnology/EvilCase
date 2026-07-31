namespace EvilBrains.Logging.WebAssembly;

internal interface IClientIdentity
{
    /// <summary>
    /// Survives reloads and browser restarts.
    /// </summary>
    public string MachineId { get; }

    /// <summary>
    /// One value per application load.
    /// </summary>
    public string SessionId { get; }
}
