using EvilBrains.EvilCase.Business.Cases;
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
        services.AddScoped<ICaseCommentWriter, CaseCommentWriter>();

        return services;
    }

    public static IHealthChecksBuilder AddEvilCaseBusinessHealthChecks(this IHealthChecksBuilder builder, params string[] tags)
    {
        builder.AddEvilCaseDataHealthChecks(tags);

        return builder;
    }
}
