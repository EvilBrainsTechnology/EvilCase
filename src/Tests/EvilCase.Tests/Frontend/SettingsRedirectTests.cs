using Bunit;
using EvilBrains.EvilCase.App.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SettingsRedirectTests
{
    [Test]
    public void TheSettingsRootOpensTheLabels()
    {
        using var ctx = new BunitContext();

        ctx.Render<Settings>();

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();

        Assert.That(navigation.ToBaseRelativePath(navigation.Uri), Is.EqualTo("settings/labels"));
    }
}
