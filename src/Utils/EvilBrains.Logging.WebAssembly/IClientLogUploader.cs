using EvilBrains.Logging.Contract;

namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// Ships one batch to the server. The implementation belongs to the application, because the API
/// client is generated per API; transport failures must surface as <see cref="ClientLogUploadException"/>.
/// </summary>
public interface IClientLogUploader
{
    public Task Upload(ClientLogBatch batch);
}
