using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Tests.Routing;

[ApiController]
public class PlaceholderController : ControllerBase, IPlaceholderApi
{
    public Task<string> GetAsync(int id, CancellationToken token = default) => Task.FromResult("");
}
