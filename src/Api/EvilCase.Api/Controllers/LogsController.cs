using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Logs;
using EvilBrains.EvilCase.Api.Logging;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("logs")]
public class LogsController(ClientLogWriter writer) : ControllerBase
{
    private const int MaxBatchSize = 4 * 1024 * 1024;

    [HttpPost("client")]
    [RequestSizeLimit(MaxBatchSize)]
    public void WriteClientLogs([FromBody] ClientLogBatch batch)
    {
        foreach (var entry in batch.Entries)
            writer.Write(entry);
    }
}
