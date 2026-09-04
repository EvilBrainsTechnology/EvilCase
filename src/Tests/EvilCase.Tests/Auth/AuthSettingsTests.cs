using EvilBrains.EvilCase.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// A compose file interpolating an unset variable hands over an empty string, not nothing, so a blank
/// seed reads as no seed.
/// </summary>
public class AuthSettingsTests
{
    private const string Section = "Auth";

    [Test]
    public void BlankSeedCredentialsAreReadAsNoSeedAtAll()
    {
        var settings = Bind(seedEmail: "", seedPassword: "");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Validate(settings).Failed, Is.False);
            Assert.That(settings.Seed?.Email, Is.Null);
            Assert.That(settings.Seed?.Password, Is.Null);
        }
    }

    [Test]
    public void AMalformedSeedEmailStillFailsTheStart()
    {
        var result = Validate(Bind(seedEmail: "not-an-email", seedPassword: "seeded-administrator"));

        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void ASeedPasswordShorterThanTheMinimumStillFailsTheStart()
    {
        var result = Validate(Bind(seedEmail: "admin@evilcase.test", seedPassword: "short"));

        Assert.That(result.Failed, Is.True);
    }

    [Test]
    public void AConfiguredSeedPassesAsItIs()
    {
        var settings = Bind(seedEmail: "admin@evilcase.test", seedPassword: "seeded-administrator");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Validate(settings).Failed, Is.False);
            Assert.That(settings.Seed?.Email, Is.EqualTo("admin@evilcase.test"));
        }
    }

    private static ValidateOptionsResult Validate(AuthSettings settings)
    {
        return new AuthSettingsValidator().Validate(name: null, settings);
    }

    private static AuthSettings Bind(string seedEmail, string seedPassword)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    [$"{Section}:Jwt:Issuer"] = "https://auth.evilcase.test",
                    [$"{Section}:Jwt:Audience"] = "EvilCase",
                    [$"{Section}:Jwt:AccessTokenExpiration"] = "00:15:00",
                    [$"{Section}:Jwt:Key"] = new string('k', 64),
                    [$"{Section}:RefreshToken:Expiration"] = "14.00:00:00",
                    [$"{Section}:RefreshToken:SessionExpiration"] = "30.00:00:00",
                    [$"{Section}:Lockout:MaxFailedAttempts"] = "5",
                    [$"{Section}:Lockout:Duration"] = "00:15:00",
                    [$"{Section}:Seed:Email"] = seedEmail,
                    [$"{Section}:Seed:Password"] = seedPassword,
                })
            .Build();

        return configuration.GetSection(Section).Get<AuthSettings>()
            ?? throw new AssertionException("The section did not bind");
    }
}
