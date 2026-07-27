namespace EvilBrains.AI.OpenAI;

public interface IOpenAIChatBot
{
    public Task<string> Chat(string prompt);
}
