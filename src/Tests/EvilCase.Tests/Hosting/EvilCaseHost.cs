using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// Settings travel as a configuration source of this factory, so coexisting hosts do not leak; only the
/// environment name must be a process variable, read before the builder exists.
/// </summary>
internal sealed class EvilCaseHost(
    bool behindReverseProxy = false,
    bool httpsRedirection = true,
    int? httpsPort = null,
    string? jwtKey = null,
    string? filesRootPath = null,
    Action<IServiceCollection>? configureServices = null) : WebApplicationFactory<Program>
{
    private static readonly string ValidJwtKey = new('k', 64);

    /// <summary>
    /// Nothing creates it; the host never touches it at startup.
    /// </summary>
    private static readonly string ValidFilesRootPath = Path.Combine(Path.GetTempPath(), "evilcase-tests-files");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // The environment is not Development, so the host does not go looking for a developer's .env.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        builder.UseEnvironment("Testing");

        // The frontend reaches the host as static web assets of the referenced Blazor project, which
        // CreateBuilder only loads in Development. Without this the app's index.html is missing and every
        // path outside the API answers 404 instead of falling back to it.
        builder.UseStaticWebAssets();

        // httpsPort: UseHttpsRedirection needs a target port; without one it logs and lets the request through.
        // jwtKey: an empty value maps to a null entry, the same as a deployment that never set it.
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                // 127.0.0.1 skips the ::1 attempt and Timeout=1 skips the default 15s wait: the
                // readiness probe still really reaches for a database that is not there, fast.
                ["EvilBrains:EvilCase:ConnectionString"] = "Host=127.0.0.1;Database=evilcase-tests;Timeout=1",
                ["EvilBrains:EvilCase:Database:MigrateOnStartup"] = "false",
                ["EvilBrains:EvilCase:Auth:Jwt:Key"] = jwtKey switch { null => ValidJwtKey, "" => null, _ => jwtKey },
                ["EvilBrains:EvilCase:Files:RootPath"] = filesRootPath switch { null => ValidFilesRootPath, "" => null, _ => filesRootPath },
                ["EvilBrains:EvilCase:Hosting:BehindReverseProxy"] = behindReverseProxy ? "true" : "false",
                ["EvilBrains:EvilCase:Hosting:HttpsRedirection"] = httpsRedirection ? "true" : "false",
                ["HTTPS_PORT"] = httpsPort?.ToString(CultureInfo.InvariantCulture),
            }));

        if (configureServices is not null)
            builder.ConfigureTestServices(configureServices);
    }
}
