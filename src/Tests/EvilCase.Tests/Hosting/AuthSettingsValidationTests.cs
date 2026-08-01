using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// AddAuthentication names a default scheme, so the authentication middleware runs on every request — the
/// health probes and index.html included. A signing key HS256 cannot use therefore breaks everything the
/// host serves, not only the authenticated endpoints, which is why the key is validated on start instead
/// of on the first request.
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

    /// <summary>
    /// HS256 needs 256 bits of key material and rejects anything shorter at signing time.
    /// </summary>
    [Test]
    public void AKeyBelowTheMinimumLengthStopsTheStart()
    {
        using var host = new EvilCaseHost(jwtKey: new string('k', 31));

        var exception = Assert.Throws<OptionsValidationException>(() => host.CreateClient());

        Assert.That(exception?.Message, Does.Contain(KeyMember));
    }
}
