using Microsoft.Extensions.Configuration;

namespace EvilBrains.Secrets.Env;

internal sealed class EnvFileConfigurationSource : FileConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        this.EnsureDefaults(builder);
        return new EnvFileConfigurationProvider(this);
    }
}
