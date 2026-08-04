using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EvilBrains.EvilCase.Business;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseBusiness(this IServiceCollection services)
    {
        services.AddEvilCaseData();

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ICaseReader, CaseReader>();
        services.AddScoped<INumberIssuer, NumberIssuer>();
        services.AddScoped<INumberingSettingsReader, NumberingSettingsReader>();
        services.AddScoped<INumberSequenceAllocator, NumberSequenceAllocator>();

        return services;
    }

    public static IHealthChecksBuilder AddEvilCaseBusinessHealthChecks(this IHealthChecksBuilder builder, params string[] tags)
    {
        builder.AddEvilCaseDataHealthChecks(tags);

        return builder;
    }
}
