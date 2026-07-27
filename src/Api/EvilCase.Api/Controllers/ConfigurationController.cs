using EvilBrains.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[Route("configuration")]
[Authorize]
public class ConfigurationController(IConfiguration configuration) : Controller
{
    [HttpGet]
    public IReadOnlyDictionary<string, string?> List()
    {
        return configuration.AsEnumerable().AsReadOnlyDictionary(x => x.Key, x => x.Value);
    }
}
