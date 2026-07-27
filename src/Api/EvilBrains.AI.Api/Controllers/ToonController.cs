using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ToonSharp;

namespace EvilBrains.AI.Api.Controllers;

[ApiController]
[Route("toon-converter")]
public class ToonController : Controller
{
    [HttpPost("json-to-toon")]
    [Consumes(MediaTypeNames.Application.Json)]
    public async Task<ContentResult> JsonToToon()
    {
        // allow repeated reading of body
        this.Request.EnableBuffering();

        var deserializedJson = await JsonSerializer.DeserializeAsync<object>(this.Request.Body, JsonSerializerOptions.Default);

        // return the stream position to the beginning so that the binder model / other parts of the pipeline can read the body
        this.Request.Body.Position = 0;

        var toon = ToonSerializer.Serialize(deserializedJson);
        return this.Content(toon, MediaTypeNames.Text.Plain, Encoding.UTF8);
    }

    [HttpPost("toon-to-json")]
    [Consumes(MediaTypeNames.Text.Plain)]
    public async Task<JsonResult> ToonToJson()
    {
        // allow repeated reading of body
        this.Request.EnableBuffering();

        var deserializedToon = await ToonSerializer.DeserializeAsync<object>(this.Request.Body);

        // return the stream position to the beginning so that the binder model / other parts of the pipeline can read the body
        this.Request.Body.Position = 0;

        return this.Json(deserializedToon);
    }
}
