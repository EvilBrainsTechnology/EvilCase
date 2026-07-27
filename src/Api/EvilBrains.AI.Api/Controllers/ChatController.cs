using EvilBrains.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.AI.Api.Controllers;

[ApiController]
[Route("chat")]
public class ChatController : Controller
{
    [HttpGet]
    public async Task<string> Index(
        [FromServices] IOpenAIChatBot chatBot,
        [FromQuery] string prompt)
    {
        return await chatBot.Chat(prompt);
    }
}
