using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Data;

public class ConnectionStringTests
{
    [Test]
    public void TheContextDisablesGssEncryption()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["EvilBrains:EvilCase:ConnectionString"] = "Host=127.0.0.1;Database=evilcase-tests" })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton(Substitute.For<IHostEnvironment>())
            .AddScoped<IUserContext, StubUserContext>()
            .AddEvilCaseData();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var connectionString = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString());

        Assert.That(
            connectionString.GssEncryptionMode,
            Is.EqualTo(GssEncryptionMode.Disable),
            "the connection must not probe GSS encryption, which loads a native library the Alpine image lacks");
    }
}
