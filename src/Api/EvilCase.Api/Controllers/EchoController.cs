using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("echo")]
public class EchoController : ControllerBase
{
    [HttpGet("get")]
    public Task<EchoResponse> EchoGet([FromQuery] EchoRequest request) =>
        Task.FromResult(new EchoResponse { Message = $"Echo: {request.Message}" });

    [HttpPost("post")]
    public Task<EchoResponse> EchoPost([FromBody] EchoRequest request) =>
        Task.FromResult(new EchoResponse { Message = $"Echo: {request.Message}" });
}
