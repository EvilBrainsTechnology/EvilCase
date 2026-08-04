using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EvilBrains.EvilCase.Business;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseBusiness(this IServiceCollection services, string caseSettingsPath)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEvilCaseData();

        services.AddScoped<ICaseReader, CaseReader>();

        // Bound through the service provider rather than with BindConfiguration, which lives in the
        // ASP.NET Core shared framework this project deliberately does not reference.
        services
            .AddOptions<CaseSettings>()
            .Configure<IConfiguration>((settings, configuration) => configuration.GetSection(caseSettingsPath).Bind(settings))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ReferenceFormat), "A case reference format is required.")
            .Validate(settings => settings.ReferenceFormat.Contains('X', StringComparison.Ordinal), "A case reference format needs a run of X for the counter, or every case of a day would get the same mark.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICaseReferenceGenerator, CaseReferenceGenerator>();

        return services;
    }

    public static IHealthChecksBuilder AddEvilCaseBusinessHealthChecks(this IHealthChecksBuilder builder, params string[] tags)
    {
        builder.AddEvilCaseDataHealthChecks(tags);

        return builder;
    }
}
