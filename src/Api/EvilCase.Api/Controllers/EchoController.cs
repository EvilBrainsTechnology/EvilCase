using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Echo;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("echo")]
public class EchoController(ILogger<EchoController> logger) : ControllerBase
{
    [HttpGet("get")]
    public Task<EchoResponse> EchoGet([FromQuery] EchoRequest request)
    {
        logger.LogInformation("Processing echo get request. (data: {Message})", request.Message);

        return Task.FromResult(new EchoResponse { Message = $"Echo: {request.Message}" });
    }

    [HttpPost("post")]
    public Task<EchoResponse> EchoPost([FromBody] EchoRequest request)
    {
        logger.LogWarning("Processing echo post request. (data: {Message})", request.Message);

        return Task.FromResult(new EchoResponse { Message = $"Echo: {request.Message}" });
    }
}
