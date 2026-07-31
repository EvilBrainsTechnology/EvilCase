using InfisicalConfiguration;
using Microsoft.Extensions.Configuration;

namespace EvilBrains.Secrets.Infisical;

public static class Bootstrap
{
    public static IConfigurationBuilder AddInfisicalSecrets(this IConfigurationBuilder builder, ConfigurationManager configuration, string settingsPath)
    {
        var settings = configuration
            .GetRequiredSection(settingsPath)
            .Get<InfisicalSettings>(options => options.ErrorOnUnknownConfiguration = true)
            ?? throw new InvalidOperationException($"Missing {settingsPath} configuration settings");

        builder.AddInfisical(
            new InfisicalConfigBuilder()
                .SetInfisicalUrl(settings.Url)
                .SetProjectId(settings.ProjectId)
                .SetEnvironment(settings.Environment)
                .SetAuth(
                    new InfisicalAuthBuilder()
                        .SetUniversalAuth(settings.ClientId, settings.ClientSecret)
                        .Build())
                .Build());

        return configuration;
    }
}
