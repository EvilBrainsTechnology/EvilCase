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

    public EvilCaseHost()
    {
        // The environment is not Development, so the host does not go looking for a developer's .env.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        // Nothing here opens a connection: the DbContext is registered, never used.
        Environment.SetEnvironmentVariable(ConnectionStringVariable, "Host=localhost;Database=evilcase-tests");
        Environment.SetEnvironmentVariable(JwtKeyVariable, new string('k', 64));
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
