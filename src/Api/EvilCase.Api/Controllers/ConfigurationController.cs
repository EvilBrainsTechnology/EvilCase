using EvilBrains.Collections;
using EvilBrains.EvilCase.Api.ActionFilters;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[Route("configuration")]
[DevelopmentOnly]
public class ConfigurationController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("")]
    public IReadOnlyDictionary<string, string?> Configuration()
    {
        return configuration
            .AsEnumerable()
            .OrderBy(x => x.Key)
            .AsReadOnlyDictionary(x => x.Key, x => x.Value);
    }
}
