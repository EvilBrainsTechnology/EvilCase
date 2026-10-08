using Bunit;
using EvilBrains.EvilCase.App.Clipboard;
using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CopyButtonRenderTests
{
    private const string Value = "EC/20260807-001/20260807-002";

    [Test]
    public async Task ACopyWritesTheValueAndShowsTheCheckForAMoment()
    {
        await using var ctx = new BunitContext();

        var clipboard = new StubClipboardWriter();
        ctx.Services.AddSingleton<IClipboardWriter>(clipboard);

        var component = RenderButton(ctx);

        var copying = component.Find("button.ec-copy").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(clipboard.Written, Is.EqualTo([Value]));
            Assert.That(component.Find("button.ec-copy").GetAttribute("aria-label"), Is.EqualTo("Zkopírováno"), "the button says the value is copied");
            Assert.That(component.Find("[role=status]").TextContent, Is.EqualTo("Zkopírováno"), "a screen reader hears the copy");
        }

        await copying;

        Assert.That(
            component.Find("button.ec-copy").GetAttribute("aria-label"),
            Is.EqualTo("Zkopírovat číslo jednací"),
            "the check shows only for a moment");
    }

    [Test]
    public async Task AFailedCopyKeepsTheLabelAndLogsAWarning()
    {
        await using var ctx = new BunitContext();

        ctx.Services.AddSingleton<IClipboardWriter>(new StubClipboardWriter { Failure = new JSException("Clipboard write denied") });

        var logger = new CapturingLogger<CopyButton>();
        ctx.Services.AddSingleton<ILogger<CopyButton>>(logger);

        var component = RenderButton(ctx);

        await component.Find("button.ec-copy").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("button.ec-copy").GetAttribute("aria-label"), Is.EqualTo("Zkopírovat číslo jednací"), "a copy that failed never claims it succeeded");
            Assert.That(component.Find("[role=status]").TextContent, Is.Empty);
            Assert.That(logger.LoggedWarningOrAbove, Is.True);
        }
    }

    private static IRenderedComponent<CopyButton> RenderButton(BunitContext ctx)
    {
        return ctx.Render<CopyButton>(static parameters => parameters
            .Add(static button => button.Value, Value)
            .Add(static button => button.AriaLabel, "Zkopírovat číslo jednací"));
    }
}
