using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseFilesModalRenderTests
{
    [Test]
    public async Task TheTreeOpensTheCasesOwnActsAndKeepsItsSubCasesClosed()
    {
        await using var ctx = FrontendContext.Create();

        var tree = Serve(ctx);

        var component = Render(ctx, tree.Case.CaseId);

        await component.WaitForElementAsync(".ec-tree");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(RowTitles(component), Is.EqualTo(["Podspis", "Odvolání", "odvolani.pdf", "priloha.pdf"]), "the sub-cases come first, closed, then the case's own acts, open");
            Assert.That(component.Find(".ec-tree-row-case").GetAttribute("aria-expanded"), Is.EqualTo("false"));
        }
    }

    [Test]
    public async Task ExpandingAndCollapsingAllReachEveryLevel()
    {
        await using var ctx = FrontendContext.Create();

        var tree = Serve(ctx);

        var component = Render(ctx, tree.Case.CaseId);

        await component.WaitForElementAsync(".ec-tree");
        await component.Find(".ec-case-files-expand button").ClickAsync(new MouseEventArgs());

        var expanded = RowTitles(component);

        await component.Find(".ec-case-files-actions button[aria-label='Sbalit vše']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(expanded, Is.EqualTo(["Podspis", "Rozhodnutí", "rozhodnuti.pdf", "Odvolání", "odvolani.pdf", "priloha.pdf"]), "expanding all opens the sub-cases and their acts");
            Assert.That(RowTitles(component), Is.EqualTo(["Podspis", "Odvolání"]), "collapsing all leaves the top level only");
        }
    }

    [Test]
    public async Task AClickOnAnActRowTogglesItsFiles()
    {
        await using var ctx = FrontendContext.Create();

        var tree = Serve(ctx);

        var component = Render(ctx, tree.Case.CaseId);

        await component.WaitForElementAsync(".ec-tree");
        await component.Find(".ec-tree-row-act").ClickAsync(new MouseEventArgs());

        var collapsed = component.FindAll(".ec-tree-row-file").Count;

        await component.Find(".ec-tree-row-act").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(collapsed, Is.Zero);
            Assert.That(component.FindAll(".ec-tree-row-file"), Has.Count.EqualTo(2), "a second click opens the act again");
        }
    }

    [Test]
    public async Task AFailedLoadOffersARetry()
    {
        await using var ctx = FrontendContext.Create();

        var tree = Tree();
        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .GetCaseFileTree(tree.Case.CaseId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CaseFileTree>(new HttpRequestException()), Task.FromResult(tree));
        ctx.Services.AddSingleton(casesClient);

        var component = Render(ctx, tree.Case.CaseId);

        await component.WaitForElementAsync(".ec-card-error");
        await component.Find(".ec-card-error button").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(RowTitles(component), Is.EqualTo(["Podspis", "Odvolání", "odvolani.pdf", "priloha.pdf"]), "the retried tree opens as a first load does"));
    }

    [Test]
    public async Task AFilePreviewPagesOverTheFilesOfItsActOnly()
    {
        await using var ctx = FrontendContext.Create();

        var tree = Serve(ctx);
        var act = tree.Acts[0];

        var actFilesClient = Substitute.For<IActFilesClient>();
        actFilesClient.ListActFiles(act.Act.ActId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = act.Files }));
        ctx.Services.AddSingleton(actFilesClient);
        ctx.Services.AddSingleton(Substitute.For<IFileVersionsClient>());
        ctx.Services.AddSingleton(Substitute.For<IFilesClient>());
        ctx.Services.AddSingleton(Substitute.For<IMainFileClient>());

        var component = Render(ctx, tree.Case.CaseId);

        await component.WaitForElementAsync(".ec-tree");
        await component.Find(".ec-tree-file-name").ClickAsync(new MouseEventArgs());
        await component.WaitForElementAsync("button[aria-label='Další soubor']");
        await component.Find("button[aria-label='Další soubor']").ClickAsync(new MouseEventArgs());

        var preview = component.FindComponent<FilePreviewModal>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(preview.Instance.Files.Select(static file => file.FileId), Is.EqualTo(act.Files.Select(static file => file.FileId)), "the preview pages over the act's files");
            Assert.That(preview.Instance.FileId, Is.EqualTo(act.Files[1].FileId), "a step shows the act's next file");
        }
    }

    private static CaseListItem Case(string title)
    {
        return new CaseListItem
        {
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260821-001",
            Title = title,
            Date = new DateOnly(2026, 8, 21),
            Status = CaseStatus.Active,
            Changed = DateTime.UtcNow,
        };
    }

    private static CaseFileTree Serve(BunitContext ctx)
    {
        var tree = Tree();

        var casesClient = Substitute.For<ICasesClient>();
        casesClient.GetCaseFileTree(tree.Case.CaseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(tree));
        ctx.Services.AddSingleton(casesClient);

        return tree;
    }

    private static CaseFileTree Tree()
    {
        var root = Case("Přestupek");
        var subCase = Case("Podspis");

        return new CaseFileTree
        {
            Case = root,
            SubCases = [new CaseFileTree { Case = subCase, Acts = [Act(subCase, "Rozhodnutí", "rozhodnuti.pdf")] }],
            Acts = [Act(root, "Odvolání", "odvolani.pdf", "priloha.pdf")],
        };
    }

    private static ActFileTree Act(CaseListItem owner, string title, params string[] fileNames)
    {
        return new ActFileTree
        {
            Act = new ActListItem
            {
                ActId = Guid.CreateVersion7(),
                CaseId = owner.CaseId,
                CaseNumber = owner.CaseNumber,
                CaseTitle = owner.Title,
                ActNumber = "1",
                Title = title,
                Date = new DateOnly(2026, 8, 21),
                Changed = DateTime.UtcNow,
            },
            Files =
            [
                .. fileNames.Select(static fileName => new FileListItem
                {
                    FileId = Guid.CreateVersion7(),
                    FileName = fileName,
                    SizeBytes = 1024,
                    Created = DateTime.UtcNow,
                    Version = 1,
                    VersionCount = 1,
                }),
            ],
        };
    }

    private static IRenderedComponent<CaseFilesModal> Render(BunitContext ctx, Guid caseId)
    {
        return ctx.Render<CaseFilesModal>(parameters => parameters.Add(static modal => modal.CaseId, caseId));
    }

    private static List<string> RowTitles(IRenderedComponent<CaseFilesModal> component)
    {
        return
        [
            .. component
                .FindAll(".ec-tree-row .ec-tree-name")
                .Select(static name => name.TextContent),
        ];
    }
}
