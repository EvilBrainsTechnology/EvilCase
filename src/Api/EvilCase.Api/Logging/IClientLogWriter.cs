using EvilBrains.Logging.Contract;

namespace EvilBrains.EvilCase.Api.Logging;

public interface IClientLogWriter
{
    public void Write(ClientLogEntry entry);
}
