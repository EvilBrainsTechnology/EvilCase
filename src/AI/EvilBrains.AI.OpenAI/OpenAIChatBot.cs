using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace EvilBrains.AI.OpenAI;

public class OpenAIChatBot(IConfiguration configuration) : IOpenAIChatBot
{
    private const string DefaultModel = "gpt-4o";

    public async Task<string> Chat(string prompt)
    {
        var openAIKeySection = configuration.GetRequiredSection("EvilBrains:Secrets:OpenAIKey");

        var client = new ChatClient(
            model: DefaultModel,
            apiKey: openAIKeySection.Value);

        var completion = await client.CompleteChatAsync(prompt);

        return completion.Value.Content[0].Text;
    }
}
