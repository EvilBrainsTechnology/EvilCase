namespace EvilBrains.EvilCase.Api.HealthChecks;

internal static class HealthCheckPaths
{
    public const string Prefix = "/health";

    public const string Live = Prefix + "/live";

    public const string Ready = Prefix + "/ready";
}
