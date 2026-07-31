using EvilBrains.Logging.Contract;

namespace EvilBrains.Logging.AspNetCore;

public interface IClientLogWriter
{
    public void Write(ClientLogEntry entry);
}
