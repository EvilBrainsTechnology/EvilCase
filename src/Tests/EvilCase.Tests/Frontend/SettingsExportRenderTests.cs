using Bunit;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SettingsExportRenderTests
{
    [Test]
    public void TheExportSectionRendersAsTheCurrentSection()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IFileDownloader>(new StubFileDownloader());
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/settings/export");

        var component = ctx.Render<SettingsExport>();

        Assert.That(component.Find("[aria-current=page]").TextContent.Trim(), Is.EqualTo("Export"), "the nav marks only the section shown");
    }
}
