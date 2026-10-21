using Microsoft.Extensions.Configuration;

namespace EvilBrains.EvilCase.Tests.Hosting;

public class RefreshTokenLifetimeTests
{
    private const string Section = "EvilBrains:EvilCase:Auth:RefreshToken";

    [Test]
    public void TheShippedSettingsKeepATokenThirtyDaysAndASessionNinetyDays()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
            .Build();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(configuration.GetValue<TimeSpan>($"{Section}:Expiration"), Is.EqualTo(TimeSpan.FromDays(30)));
            Assert.That(configuration.GetValue<TimeSpan>($"{Section}:SessionExpiration"), Is.EqualTo(TimeSpan.FromDays(90)));
        }
    }
}
