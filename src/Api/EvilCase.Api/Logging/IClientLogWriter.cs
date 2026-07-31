using EvilBrains.EvilCase.Api.Contract.Logs;

namespace EvilBrains.EvilCase.Api.Logging;

public interface IClientLogWriter
{
    public void Write(ClientLogEntry entry);
}
