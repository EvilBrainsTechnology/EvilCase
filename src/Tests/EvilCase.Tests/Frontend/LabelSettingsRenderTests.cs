using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class LabelSettingsRenderTests
{
    [Test]
    public void TheLabelSettingsRender()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var label = new LabelListItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue, CaseCount = 4, ActCount = 1 };

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [label] }));

        ctx.Services.AddSingleton(labelsClient);

        var component = ctx.Render<LabelSettings>();

        component.WaitForAssertion(() => Assert.That(component.FindComponents<MetaLine>(), Has.Count.EqualTo(1)));
    }
}
