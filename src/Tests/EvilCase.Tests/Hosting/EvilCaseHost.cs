using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// Runs the real host: its Program.cs, its middleware pipeline and its endpoints. Per-instance settings
/// travel as a configuration source of this factory, so coexisting hosts cannot leak them into each
/// other. The environment name is the one exception: the host checks it before the builder exists, so it
/// has to be a process environment variable — safe process-wide because every instance writes the same
/// value.
/// </summary>
internal sealed class EvilCaseHost(
    bool behindReverseProxy = false,
    bool httpsRedirection = true,
    int? httpsPort = null,
    string? jwtKey = null,
    string? filesRootPath = null,
    Action<IServiceCollection>? configureServices = null) : WebApplicationFactory<Program>
{
    /// <summary>
    /// Long enough to pass the signing key validation, so every test that is not about the key gets a host
    /// that starts.
    /// </summary>
    private static readonly string ValidJwtKey = new('k', 64);

    /// <summary>
    /// A directory every test that is not about the file storage root gets a host that starts with.
    /// Nothing creates it; the host never touches it at startup.
    /// </summary>
    private static readonly string ValidFilesRootPath = Path.Combine(Path.GetTempPath(), "evilcase-tests-files");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // The environment is not Development, so the host does not go looking for a developer's .env.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        builder.UseEnvironment("Testing");

        // The frontend reaches the host as static web assets of the referenced Blazor project, which
        // CreateBuilder only loads in Development. Without this the app's index.html is missing and every
        // path outside the API answers 404 instead of falling back to it.
        builder.UseStaticWebAssets();

        // Nothing here opens a connection: the DbContext is registered, never used, and startup migration
        // — the one thing that would reach for the database before a request arrives — is turned off.
        // httpsPort: UseHttpsRedirection needs a target port; without one it logs and lets the request
        // through, so a test that wants to see a redirect has to name it.
        // jwtKey: an empty value maps to a null entry, which leaves the key unconfigured — the same
        // failure as a deployment that never set it.
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["EvilBrains:EvilCase:ConnectionString"] = "Host=localhost;Database=evilcase-tests",
                ["EvilBrains:EvilCase:Database:MigrateOnStartup"] = "false",
                ["EvilBrains:EvilCase:Auth:Jwt:Key"] = jwtKey switch { null => ValidJwtKey, "" => null, _ => jwtKey },
                ["EvilBrains:EvilCase:Files:RootPath"] = filesRootPath switch { null => ValidFilesRootPath, "" => null, _ => filesRootPath },
                ["EvilBrains:EvilCase:Hosting:BehindReverseProxy"] = behindReverseProxy ? "true" : "false",
                ["EvilBrains:EvilCase:Hosting:HttpsRedirection"] = httpsRedirection ? "true" : "false",
                ["HTTPS_PORT"] = httpsPort?.ToString(CultureInfo.InvariantCulture),
            }));

        // Runs after the application's own registrations, so a test that wants to reach an endpoint which
        // does touch the database swaps the two stores that do.
        if (configureServices is not null)
            builder.ConfigureTestServices(configureServices);
    }
}
