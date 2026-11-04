using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Components.Lists;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseListRenderTests
{
    [Test]
    public async Task ClickingTheSortedHeaderFlipsTheDirectionAndAsksForTheFirstPageAgain()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, total: 3);

        var component = Render(ctx, [CaseColumn.Date], take: 2, paging: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-card-footer button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find(".ec-table-header button").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].SortDirection, Is.EqualTo(ListSortDirection.Ascending), "a click on the sorted column turns it around");
            Assert.That(requests[^1].Skip, Is.Zero, "a change of the order starts the list over");
        }
    }

    [Test]
    public async Task ChangingTheSearchResetsTheListToItsFirstPage()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, total: 3);

        var component = Render(ctx, [CaseColumn.Date], take: 2, paging: true, search: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-card-footer button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "kolo" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].Search, Is.EqualTo("kolo"));
            Assert.That(requests[^1].Skip, Is.Zero, "a new search starts the list over");
        }
    }

    [Test]
    public async Task TheFilterTheHostGaveSurvivesEveryReloadTheToolbarTriggers()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var contactId = Guid.CreateVersion7();
        var component = Render(ctx, [CaseColumn.Date], contactId: contactId);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-table-header button").ClickAsync(new MouseEventArgs());

        Assert.That(
            requests.Select(static request => request.ContactId),
            Is.All.EqualTo(contactId),
            "the toolbar narrows what the host's filter left, never widens it");
    }

    [Test]
    public async Task AnEqualFilterHandedAgainLoadsNothingAgain()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var component = Render(ctx, [CaseColumn.Date]);

        await component.WaitForElementAsync("a.ec-table-row");

        // The host builds its filter in a property, so every one of its renders hands over a new
        // record of the same values.
        component.Render(static parameters => parameters.Add(static list => list.Filter, new CaseListRequest { Take = 20 }));

        Assert.That(requests, Has.Count.EqualTo(1), "a filter the list already loaded reloads nothing");
    }

    [Test]
    public async Task AHostFilterWithAnotherStatusResetsTheSwitch()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var component = Render(ctx, [CaseColumn.Date], statusSwitch: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find("#cases-status-Closed").ClickAsync(new MouseEventArgs());

        component.Render(static parameters => parameters.Add(static list => list.Filter, new CaseListRequest { Take = 20, Status = CaseStatusFilter.Active }));

        await component.WaitForAssertionAsync(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#cases-status-Active").GetAttribute("aria-pressed"), Is.EqualTo("true"), "a new address narrows the list and the switch with it");
                Assert.That(requests[^1].Status, Is.EqualTo(CaseStatusFilter.Active));
            }
        });
    }

    [Test]
    public async Task ClearingTheFiltersIsOfferedOnlyWhileAFilterIsSet()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [CaseColumn.Date], dates: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find("button[aria-controls=\"cases-filters\"]").ClickAsync(new MouseEventArgs());

        var unfiltered = component.FindAll(".ec-filter-clear").Count;

        await component.Find("#cases-from").ChangeAsync(new ChangeEventArgs { Value = "2026-08-01" });

        var filtered = component.FindAll(".ec-filter-clear").Count;

        await component.Find(".ec-filter-clear").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unfiltered, Is.Zero, "a list without a filter has nothing to clear");
            Assert.That(filtered, Is.EqualTo(1));
            Assert.That(component.FindAll(".ec-filter-clear"), Is.Empty, "the cleared filters take the action with them");
        }
    }

    [Test]
    public async Task TheMainFileButtonPreviewsThatFileAlone()
    {
        await using var ctx = FrontendContext.Create();
        var (_, mainFile, withMain) = ServeMainFile(ctx);

        var component = Render(ctx, [CaseColumn.Case, CaseColumn.MainFile]);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-table-row"), Has.Count.EqualTo(2)));

        var rows = component.FindAll(".ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows[0].QuerySelector("button.ec-main-file")?.GetAttribute("aria-label"), Is.EqualTo("rozhodnuti.pdf"), "the button names the main file");
            Assert.That(rows[1].QuerySelector("button.ec-main-file"), Is.Null, "a case without a main file has an empty cell");
        }

        await rows[0].QuerySelector("button.ec-main-file")!.ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindComponents<FilePreviewModal>(), Has.Count.EqualTo(1)));

        var preview = component.FindComponent<FilePreviewModal>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(preview.Instance.FileId, Is.EqualTo(mainFile.FileId));
            Assert.That(preview.Instance.Files.Select(static file => file.FileId), Is.EqualTo([mainFile.FileId]), "the preview pages over no other file of the case");
            Assert.That(component.FindComponent<MainFilePreview>().Instance.OwnerId, Is.EqualTo(withMain));
        }
    }

    [Test]
    public async Task UnsettingTheMainFileInThePreviewReloadsTheList()
    {
        await using var ctx = FrontendContext.Create();
        var (casesClient, mainFile, _) = ServeMainFile(ctx);
        var mainFiles = ctx.Services.GetRequiredService<IMainFileClient>();

        var component = Render(ctx, [CaseColumn.Case, CaseColumn.MainFile]);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("button.ec-main-file"), Has.Count.EqualTo(1)));
        await component.Find("button.ec-main-file").ClickAsync(new MouseEventArgs());
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-name-row .ec-main-star"), Has.Count.EqualTo(1)));

        await component.Find(".ec-preview-name-row .ec-main-star").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(
            casesClient.ReceivedCalls().Count(static call => string.Equals(call.GetMethodInfo().Name, nameof(ICasesClient.ListCases), StringComparison.Ordinal)),
            Is.EqualTo(2),
            "the list reloads after the change"));

        await mainFiles.Received(1).UnsetMainFile(mainFile.FileId, Arg.Any<CancellationToken>());
    }

    private static (ICasesClient CasesClient, FileListItem MainFile, Guid WithMain) ServeMainFile(BunitContext ctx)
    {
        var mainFile = CaseFile("rozhodnuti.pdf") with { IsMain = true };
        var withMain = Guid.NewGuid();
        var cases = new CaseListResponse
        {
            Items =
            [
                Row with { CaseId = withMain, MainFile = new MainFile { FileId = mainFile.FileId, FileName = mainFile.FileName } },
                Row with { CaseId = Guid.NewGuid(), Title = "Odvolání" },
            ],
            TotalCount = 2,
        };

        var casesClient = Substitute.For<ICasesClient>();
        casesClient.ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(cases));

        var caseFilesClient = Substitute.For<ICaseFilesClient>();
        caseFilesClient.ListCaseFiles(withMain, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FileListResponse { Items = [mainFile, CaseFile("priloha.pdf")] }));

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(caseFilesClient);
        ctx.Services.AddSingleton(Substitute.For<IActFilesClient>());
        ctx.Services.AddSingleton(Substitute.For<IFileVersionsClient>());
        ctx.Services.AddSingleton(Substitute.For<IMainFileClient>());

        return (casesClient, mainFile, withMain);
    }

    private static FileListItem CaseFile(string fileName)
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

    private static readonly CaseListItem Row = new()
    {
        CaseId = Guid.CreateVersion7(),
        CaseNumber = "EC/20260821-001",
        Title = "Přestupek",
        Date = new DateOnly(2026, 8, 21),
        Status = CaseStatus.Active,
        Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
    };

    private static void Serve(BunitContext ctx, out List<CaseListRequest> requests, int total = 1, IReadOnlyList<CaseListItem>? items = null)
    {
        var captured = new List<CaseListRequest>();
        var response = new CaseListResponse
        {
            Items = items ?? [Row],
            TotalCount = total,
        };

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured.Add(call.Arg<CaseListRequest>());

                return Task.FromResult(response);
            });

        ctx.Services.AddSingleton(casesClient);

        requests = captured;
    }

    private static IRenderedComponent<CaseList> Render(
        BunitContext ctx,
        IReadOnlyList<CaseColumn> columns,
        int take = 20,
        bool paging = false,
        bool search = false,
        bool scopeFilter = false,
        bool statusFilter = false,
        bool statusSwitch = false,
        bool dates = false,
        bool labelFilter = false,
        Guid? contactId = null)
    {
        return ctx.Render<CaseList>(parameters => parameters
            .Add(static list => list.Columns, columns)
            .Add(static list => list.Filter, new CaseListRequest { ContactId = contactId, Take = take })
            .Add(static list => list.ShowPaging, paging)
            .Add(static list => list.ShowSearch, search)
            .Add(static list => list.ShowScopeFilter, scopeFilter)
            .Add(static list => list.ShowStatusFilter, statusFilter)
            .Add(static list => list.ShowStatusSwitch, statusSwitch)
            .Add(static list => list.ShowDates, dates)
            .Add(static list => list.ShowLabelFilter, labelFilter));
    }
}
