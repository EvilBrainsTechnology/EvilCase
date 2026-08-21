using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Tests.Hosting;

public class FileSettingsValidationTests
{
    [Test]
    public void AnUnconfiguredStorageRootStopsTheStart()
    {
        using var host = new EvilCaseHost(filesRootPath: "");

        var exception = Assert.Throws<OptionsValidationException>(() => host.CreateClient());

        Assert.That(exception?.Message, Does.Contain("RootPath"), "an unset root has to fail at startup, not at the first upload");
    }
}
