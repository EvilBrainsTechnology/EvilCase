using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Components.Lists;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActListRenderTests
{
    private static readonly FileListItem Main = File("rozhodnuti.pdf") with { IsMain = true };

    private static readonly FileListItem Other = File("priloha.pdf");

    [Test]
    public async Task TheMainFileButtonPreviewsThatFileAlone()
    {
        await using var ctx = FrontendContext.Create();
        var (_, withMain) = Serve(ctx);

        var component = Render(ctx);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-table-row"), Has.Count.EqualTo(2)));

        var rows = component.FindAll(".ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows[0].QuerySelector("button.ec-main-file")?.GetAttribute("aria-label"), Is.EqualTo("rozhodnuti.pdf"), "the button names the main file");
            Assert.That(rows[1].QuerySelector("button.ec-main-file"), Is.Null, "an act without a main file has an empty cell");
        }

        await rows[0].QuerySelector("button.ec-main-file")!.ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindComponents<FilePreviewModal>(), Has.Count.EqualTo(1)));

        var preview = component.FindComponent<FilePreviewModal>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(preview.Instance.FileId, Is.EqualTo(Main.FileId));
            Assert.That(preview.Instance.Files.Select(static file => file.FileId), Is.EqualTo([Main.FileId]), "the preview pages over no other file of the act");
            Assert.That(component.FindComponent<ActFilePreview>().Instance.ActId, Is.EqualTo(withMain));
        }
    }

    [Test]
    public async Task UnsettingTheMainFileInThePreviewReloadsTheList()
    {
        await using var ctx = FrontendContext.Create();
        var (actsClient, _) = Serve(ctx);
        var mainFiles = ctx.Services.GetRequiredService<IMainFileClient>();

        var component = Render(ctx);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("button.ec-main-file"), Has.Count.EqualTo(1)));
        await component.Find("button.ec-main-file").ClickAsync(new MouseEventArgs());
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-name-row .ec-main-star"), Has.Count.EqualTo(1)));

        await component.Find(".ec-preview-name-row .ec-main-star").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(
            actsClient.ReceivedCalls().Count(static call => string.Equals(call.GetMethodInfo().Name, nameof(IActsClient.ListActs), StringComparison.Ordinal)),
            Is.EqualTo(2),
            "the list reloads after the change"));

        await mainFiles.Received(1).UnsetMainFile(Main.FileId, Arg.Any<CancellationToken>());
    }

    private static IRenderedComponent<ActList> Render(BunitContext ctx)
    {
        return ctx.Render<ActList>(static parameters => parameters
            .Add(static list => list.Columns, [ActColumn.ActWithLabels, ActColumn.Changed, ActColumn.MainFile])
            .Add(static list => list.Filter, new ActListRequest { Take = 20 }));
    }

    private static (IActsClient ActsClient, Guid WithMain) Serve(BunitContext ctx)
    {
        var withMain = Guid.NewGuid();
        var acts = new ActListResponse
        {
            Items = [Act(withMain, "Rozhodnutí") with { MainFile = new MainFile { FileId = Main.FileId, FileName = Main.FileName } }, Act(Guid.NewGuid(), "Výzva")],
            TotalCount = 2,
        };

        var actsClient = Substitute.For<IActsClient>();
        actsClient.ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(acts));

        var actFilesClient = Substitute.For<IActFilesClient>();
        actFilesClient.ListActFiles(withMain, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [Main, Other] }));

        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(actFilesClient);
        ctx.Services.AddSingleton(Substitute.For<IFileVersionsClient>());
        ctx.Services.AddSingleton(Substitute.For<IMainFileClient>());

        return (actsClient, withMain);
    }

    private static ActListItem Act(Guid actId, string title)
    {
        return new ActListItem
        {
            ActId = actId,
            CaseId = Guid.NewGuid(),
            CaseNumber = "EC/20261104-001",
            CaseTitle = "Spis",
            ActNumber = "EC/20261104-001/20261104-001",
            Title = title,
            Date = new DateOnly(2026, 11, 4),
            Changed = new DateTime(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc),
        };
    }

    private static FileListItem File(string fileName)
    {
        return new FileListItem
        {
            FileId = Guid.NewGuid(),
            FileName = fileName,
            SizeBytes = 1024,
            Created = new DateTime(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc),
            Version = 1,
            VersionCount = 1,
        };
    }
}
