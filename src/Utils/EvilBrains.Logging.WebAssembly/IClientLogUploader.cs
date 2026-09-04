using EvilBrains.Logging.Contract;

namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// Implemented by the application; a transport failure surfaces as ClientLogUploadException.
/// </summary>
public interface IClientLogUploader
{
    public Task Upload(ClientLogBatch batch);
}
