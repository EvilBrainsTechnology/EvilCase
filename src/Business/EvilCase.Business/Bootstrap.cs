using EvilBrains.EvilCase.Data;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Business;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseBusiness(this IServiceCollection services)
    {
        services.AddEvilCaseData();

        return services;
    }
}
