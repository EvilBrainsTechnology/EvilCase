using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.Logging.Contract;
using EvilBrains.Logging.WebAssembly;

namespace EvilBrains.EvilCase.App.Logging;

internal sealed class ApiLogUploader(ILogsClient client) : IClientLogUploader
{
    public async Task Upload(ClientLogBatch batch)
    {
        try
        {
            await client.WriteClientLogs(batch);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            throw new ClientLogUploadException("Uploading client log entries failed.", exception);
        }
    }
}
