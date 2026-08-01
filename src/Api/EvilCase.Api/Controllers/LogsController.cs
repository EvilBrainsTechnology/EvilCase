using EvilBrains.ApiClient;
using EvilBrains.Logging.AspNetCore;
using EvilBrains.Logging.Contract;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/logs")]
public class LogsController(IClientLogWriter writer) : ControllerBase
{
    // Kestrel would otherwise accept 30 MB before model validation gets to reject the batch.
    private const int MaxRequestBodyBytes = 4 * 1024 * 1024;

    [HttpPost("client")]
    [RequestSizeLimit(MaxRequestBodyBytes)]
    public void WriteClientLogs([FromBody] ClientLogBatch batch)
    {
        foreach (var entry in batch.Entries)
        {
            // A null element passes validation, which covers properties and never collection elements.
            if (entry is not null)
                writer.Write(entry);
        }
    }
}
