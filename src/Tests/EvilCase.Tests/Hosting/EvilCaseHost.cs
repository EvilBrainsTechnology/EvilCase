using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// Runs the real host: its Program.cs, its middleware pipeline and its endpoints. Configuration comes
/// from environment variables because the host reads it before the builder exists, which is past the
/// point where a WebApplicationFactory could contribute configuration sources.
/// </summary>
internal sealed class EvilCaseHost : WebApplicationFactory<Program>
{
    private const string ConnectionStringVariable = "EvilBrains__EvilCase__ConnectionString";

    private const string JwtKeyVariable = "EvilBrains__EvilCase__Auth__Jwt__Key";

    private const string MigrateOnStartupVariable = "EvilBrains__EvilCase__Database__MigrateOnStartup";

    private const string BehindReverseProxyVariable = "EvilBrains__EvilCase__Hosting__BehindReverseProxy";

    private const string HttpsRedirectionVariable = "EvilBrains__EvilCase__Hosting__HttpsRedirection";

    private const string HttpsPortVariable = "ASPNETCORE_HTTPS_PORT";

    /// <summary>
    /// Long enough to pass the signing key validation, so every test that is not about the key gets a host
    /// that starts.
    /// </summary>
    private static readonly string ValidJwtKey = new('k', 64);

    // httpsPort: UseHttpsRedirection needs a target port; without one it logs and lets the request
    // through, so a test that wants to see a redirect has to name it.
    // jwtKey: an empty value clears the variable, which leaves the key unconfigured — the same failure as
    // a deployment that never set it.
    public EvilCaseHost(
        bool behindReverseProxy = false,
        bool httpsRedirection = true,
        int? httpsPort = null,
        string? jwtKey = null)
    {
        // The environment is not Development, so the host does not go looking for a developer's .env.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        // Nothing here opens a connection: the DbContext is registered, never used, and startup migration
        // — the one thing that would reach for the database before a request arrives — is turned off.
        Environment.SetEnvironmentVariable(ConnectionStringVariable, "Host=localhost;Database=evilcase-tests");
        Environment.SetEnvironmentVariable(MigrateOnStartupVariable, "false");
        Environment.SetEnvironmentVariable(JwtKeyVariable, jwtKey ?? ValidJwtKey);

        // These reach the host through the process environment, which outlives the instance that set it,
        // so every one of them is written on every construction rather than only when it differs.
        Environment.SetEnvironmentVariable(BehindReverseProxyVariable, behindReverseProxy ? "true" : "false");
        Environment.SetEnvironmentVariable(HttpsRedirectionVariable, httpsRedirection ? "true" : "false");
        Environment.SetEnvironmentVariable(HttpsPortVariable, httpsPort?.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The frontend reaches the host as static web assets of the referenced Blazor project, which
    /// CreateBuilder only loads in Development. Without this the app's index.html is missing and every
    /// path outside the API answers 404 instead of falling back to it.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseStaticWebAssets();
    }
}
