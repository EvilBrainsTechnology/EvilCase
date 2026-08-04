using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Business;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseBusiness(this IServiceCollection services)
    {
        services.AddEvilCaseData();

        services.AddScoped<ICaseReader, CaseReader>();

        return services;
    }

    public static IHealthChecksBuilder AddEvilCaseBusinessHealthChecks(this IHealthChecksBuilder builder, params string[] tags)
    {
        builder.AddEvilCaseDataHealthChecks(tags);

        return builder;
    }
}
