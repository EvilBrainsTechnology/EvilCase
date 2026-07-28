using EvilBrains.EvilCase.Api.Client;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
public class EchoController : ControllerBase, IEchoApi
{
    public Task<EchoResponse> EchoAsync(EchoRequest request, CancellationToken token = default) =>
        Task.FromResult(new EchoResponse { Message = $"Echo: {request.Message}" });
}
