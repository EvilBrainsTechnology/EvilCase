using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.AI.OpenAI;

public static class Bootstrap
{
    public static IServiceCollection AddEvilBrainsAIOpenAI(this IServiceCollection services)
    {
        services.AddScoped<IOpenAIChatBot, OpenAIChatBot>();

        return services;
    }
}
