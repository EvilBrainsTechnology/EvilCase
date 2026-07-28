using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Tests.Routing;

[ApiController]
public class AliasController : ControllerBase, IAliasApi
{
    public Task<string> GetAsync(Guid id, CancellationToken token = default) => Task.FromResult("");
}
