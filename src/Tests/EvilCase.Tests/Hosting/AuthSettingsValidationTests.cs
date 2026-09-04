using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The authentication middleware runs on every request, health probes included, so a bad key breaks
/// everything; hence validation on start.
/// </summary>
public class AuthSettingsValidationTests
{
    private const string KeyMember = "AuthSettings.Jwt.Key";

    [Test]
    public void AnUnconfiguredKeyStopsTheStart()
    {
        using var host = new EvilCaseHost(jwtKey: "");

        var exception = Assert.Throws<OptionsValidationException>(() => host.CreateClient());

        Assert.That(exception?.Message, Does.Contain(KeyMember));
    }

    [Test]
    public void AKeyBelowTheMinimumLengthStopsTheStart()
    {
        using var host = new EvilCaseHost(jwtKey: new string('k', 31));

        var exception = Assert.Throws<OptionsValidationException>(() => host.CreateClient());

        Assert.That(exception?.Message, Does.Contain(KeyMember), "HS256 needs 256 bits of key material and rejects anything shorter at signing time");
    }
}
