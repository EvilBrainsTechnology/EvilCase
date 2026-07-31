using Microsoft.Extensions.Configuration;

namespace EvilBrains.Secrets.Env;

/// <summary>
/// Reads a <c>.env</c> file of <c>KEY=value</c> lines. Keys use the same double underscore separator
/// as environment variables, so <c>EvilBrains__EvilCase__ConnectionString</c> becomes the
/// configuration key <c>EvilBrains:EvilCase:ConnectionString</c>.
/// </summary>
internal sealed class EnvFileConfigurationProvider(EnvFileConfigurationSource source) : FileConfigurationProvider(source)
{
    public override void Load(Stream stream)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            var entry = line.Trim();
            if (entry.Length == 0 || entry.StartsWith('#'))
                continue;

            var separatorIndex = entry.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
                throw new FormatException($"Invalid line in {source.Path}: '{line}'. Expected 'KEY=value'.");

            var key = entry[..separatorIndex].Trim().Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
            data[key] = Unquote(entry[(separatorIndex + 1)..].Trim());
        }

        this.Data = data;
    }

    private static string Unquote(string value) =>
        value.Length >= 2 && (value[0] is '"' or '\'') && value[^1] == value[0]
            ? value[1..^1]
            : value;
}
