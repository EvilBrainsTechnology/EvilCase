using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.Logging.AspNetCore;
using EvilBrains.Logging.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

// The whole route sits on the controller and the action template is empty: the host and the browser sink
// both silence this exact path, and one constant is what keeps the three from drifting apart.
[ApiController]
[GenerateApiClient]
[AllowAnonymous]
[Route(ClientLogRoute.Template)]
public class LogsController : ControllerBase
{
    // Kestrel would otherwise accept 30 MB before model validation gets to reject the batch.
    private const int MaxRequestBodyBytes = 4 * 1024 * 1024;

    [HttpPost("")]
    [RequestSizeLimit(MaxRequestBodyBytes)]
    public void WriteClientLogs([FromBody] ClientLogBatch batch, [FromServices] IClientLogWriter writer)
    {
        foreach (var entry in batch.Entries)
        {
            // A null element passes validation, which covers properties and never collection elements.
            if (entry is not null)
                writer.Write(entry);
        }
    }
}
