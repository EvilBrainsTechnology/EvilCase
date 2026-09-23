using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcTableRenderTests
{
    private static readonly EcColumn<string>[] Columns =
    [
        new() { Header = "Spis", Width = "minmax(0, 1fr)", SortKey = "Title", Primary = true, Cell = static item => builder => builder.AddContent(0, item) },
        new() { Header = "Stav", Width = "150px", Cell = static item => builder => builder.AddContent(0, item) },
    ];

    [Test]
    public void TheCountBesideTheTitleBelongsToAListThatPages()
    {
        using var pagingCtx = new BunitContext();
        var paging = Render(pagingCtx, title: "Úkony", total: 5, showPaging: true);

        using var noPagingCtx = new BunitContext();
        var noPaging = Render(noPagingCtx, title: "Úkony", total: 5, showPaging: false);

        using var noTitleCtx = new BunitContext();
        var noTitle = Render(noTitleCtx, total: 5, showPaging: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(paging.Find(".ec-card-count").TextContent, Is.EqualTo("5"));
            Assert.That(
                noPaging.FindAll(".ec-card-count"),
                Is.Empty,
                "a tile that does not page shows no total count (SDD-015)");
            Assert.That(noTitle.FindAll(".ec-card-count"), Is.Empty);
        }
    }

    [Test]
    public void TheRowIsOneLinkAcrossEveryColumn()
    {
        using var ctx = new BunitContext();

        var component = Render(ctx);

        var rows = component.FindAll("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows, Has.Count.EqualTo(3));
            Assert.That(rows[0].GetAttribute("href"), Is.EqualTo("/cases/a"));
            Assert.That(rows[0].QuerySelectorAll(".ec-table-cell"), Has.Count.EqualTo(2));
        }
    }

    [Test]
    public void EveryRowRendersOnceSoOnlyCssReflowsIt()
    {
        using var ctx = new BunitContext();

        var component = Render(ctx);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-table-row"), Has.Count.EqualTo(3));
            Assert.That(
                component.FindAll(".d-lg-none, .d-none"),
                Is.Empty,
                "the narrow row reflows through CSS instead of being rendered a second time");
        }
    }

    [Test]
    public void ThePrimaryColumnMarksItsCellForTheNarrowRow()
    {
        using var ctx = new BunitContext();

        var component = Render(ctx);

        var cells = component.FindAll("a.ec-table-row")[0].QuerySelectorAll(".ec-table-cell");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cells[0].ClassList, Does.Contain("ec-table-cell-primary"));
            Assert.That(cells[1].ClassList, Does.Not.Contain("ec-table-cell-primary"));
        }
    }

    [Test]
    public void TheColumnWidthsTravelAsOneCustomPropertyOnTheTable()
    {
        using var ctx = new BunitContext();

        var component = Render(ctx);

        Assert.That(component.Find(".ec-table").GetAttribute("style"), Does.Contain("--ec-table-columns: minmax(0, 1fr) 150px"));
    }

    [Test]
    public void ASortableHeaderIsAButtonAndAnUnsortableOneIsPlainText()
    {
        using var ctx = new BunitContext();

        var component = Render(ctx);

        var headers = component.FindAll(".ec-table-header");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(headers[0].QuerySelector("button"), Is.Not.Null, "a column with a sort key sorts");
            Assert.That(headers[1].QuerySelector("button"), Is.Null, "a column without a sort key carries no button");
        }
    }

    [Test]
    public async Task ClickingASortableHeaderReportsItsKey()
    {
        await using var ctx = new BunitContext();

        string? reported = null;

        var component = Render(ctx, sortChanged: key => reported = key);

        await component.Find(".ec-table-header button").ClickAsync(new MouseEventArgs());

        Assert.That(reported, Is.EqualTo("Title"));
    }

    [Test]
    public void TheSortedHeaderShowsTheDirection()
    {
        using var ctx = new BunitContext();

        var descending = Render(ctx, sortKey: "Title", sortDescending: true);

        using var ctx2 = new BunitContext();

        var ascending = Render(ctx2, sortKey: "Title", sortDescending: false);

        using (Assert.EnterMultipleScope())
        {
            var descendingButton = descending.Find(".ec-table-header button");
            Assert.That(descendingButton.TextContent.Trim(), Does.EndWith("↓"));
            Assert.That(descendingButton.ClassList, Does.Contain("ec-sort-active"));

            Assert.That(ascending.Find(".ec-table-header button").TextContent.Trim(), Does.EndWith("↑"));
        }
    }

    [Test]
    public async Task TheSortSelectOffersEverySortableColumnAndReportsTheChosenKey()
    {
        await using var ctx = new BunitContext();

        string? reported = null;

        var component = Render(ctx, sortChanged: key => reported = key);

        Assert.That(component.FindAll(".ec-sort-select option"), Has.Count.EqualTo(1));

        await component.Find(".ec-sort-select").ChangeAsync(new ChangeEventArgs { Value = "Title" });

        Assert.That(reported, Is.EqualTo("Title"));
    }

    [Test]
    public void ABarWithNothingButTheSortSelectIsMarkedForTheNarrowWidthAlone()
    {
        using var ctx = new BunitContext();

        var withoutToolbar = Render(ctx, showToolbar: false);

        using var ctx2 = new BunitContext();

        var withToolbar = Render(ctx2, showToolbar: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withoutToolbar.Find(".ec-table-bar").ClassList, Does.Contain("ec-table-bar-sortonly"));
            Assert.That(withToolbar.Find(".ec-table-bar").ClassList, Does.Not.Contain("ec-table-bar-sortonly"));
        }
    }

    [Test]
    public void TheFooterCarriesTheRangeTheCountAndBothDirections()
    {
        using var ctx = new BunitContext();

        var component = Render(ctx, skip: 0, take: 2, total: 5, showPaging: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-table-range").TextContent, Is.EqualTo("1–2 z 5"));
            Assert.That(component.FindAll(".ec-card-footer button")[0].HasAttribute("disabled"), Is.True);
            Assert.That(component.FindAll(".ec-card-footer button")[1].HasAttribute("disabled"), Is.False);
        }
    }

    [Test]
    public void TheLastPageDisablesTheNextButton()
    {
        using var ctx = new BunitContext();

        var component = Render(ctx, skip: 4, take: 2, total: 5, showPaging: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-table-range").TextContent, Is.EqualTo("5–5 z 5"));
            Assert.That(component.FindAll(".ec-card-footer button")[1].HasAttribute("disabled"), Is.True);
        }
    }

    [Test]
    public void TheFooterStaysAwayWhereTheHostAsksForNoPaging()
    {
        using var ctx = new BunitContext();

        var component = Render(ctx, total: 5, showPaging: false);

        Assert.That(component.FindAll(".ec-card-footer"), Is.Empty);
    }

    [Test]
    public void LoadingEmptyAndFailureEachHoldTheListsHeight()
    {
        using var loadingCtx = new BunitContext();
        var loading = Render(loadingCtx, loading: true);

        using var emptyCtx = new BunitContext();
        var empty = Render(emptyCtx, items: []);

        using var failureCtx = new BunitContext();
        var failure = Render(failureCtx, failure: "Nepovedlo se");

        using (Assert.EnterMultipleScope())
        {
            foreach (var component in new[] { loading, empty, failure })
            {
                Assert.That(
                    component.FindAll(".ec-card-state, .ec-card-error, .ec-empty"),
                    Has.Count.EqualTo(1),
                    "every state holds the same height so the card does not jump");
                Assert.That(component.FindAll(".ec-table-row"), Is.Empty);
            }
        }
    }

    [Test]
    public async Task TheFailureNamesWhatFailedAndOffersARetry()
    {
        await using var ctx = new BunitContext();

        var retried = false;

        var component = Render(ctx, failure: "Nepovedlo se", retry: () => retried = true);

        Assert.That(component.Markup, Does.Contain("Nepovedlo se"));

        await component.Find(".ec-card-error button").ClickAsync(new MouseEventArgs());

        Assert.That(retried, Is.True);
    }

    private static IRenderedComponent<EcTable<string>> Render(
        BunitContext ctx,
        string? title = null,
        IReadOnlyList<string>? items = null,
        Action<string>? sortChanged = null,
        string? sortKey = null,
        bool sortDescending = false,
        bool loading = false,
        string? failure = null,
        Action? retry = null,
        bool showToolbar = false,
        int skip = 0,
        int take = 20,
        int total = 0,
        bool showPaging = false)
    {
        return ctx.Render<EcTable<string>>(parameters => parameters
            .Add(static table => table.Title, title)
            .Add(static table => table.Columns, Columns)
            .Add(static table => table.Items, items ?? ["a", "b", "c"])
            .Add(static table => table.RowHref, static item => $"/cases/{item}")
            .Add(static table => table.SortKey, sortKey)
            .Add(static table => table.SortDescending, sortDescending)
            .Add(static table => table.SortChanged, sortChanged ?? (static _ => { }))
            .Add(static table => table.Loading, loading)
            .Add(static table => table.Failure, failure)
            .Add(static table => table.Retry, retry ?? (static () => { }))
            .Add(static table => table.ShowToolbar, showToolbar)
            .Add(static table => table.Skip, skip)
            .Add(static table => table.Take, take)
            .Add(static table => table.Total, total)
            .Add(static table => table.ShowPaging, showPaging)
            .Add(static table => table.Previous, static () => { })
            .Add(static table => table.Next, static () => { })
            .Add(
                static table => table.Empty,
                static builder =>
                {
                    builder.OpenComponent<EcEmptyState>(0);
                    builder.AddComponentParameter(1, nameof(EcEmptyState.Icon), AppIcons.Folders);
                    builder.AddComponentParameter(2, nameof(EcEmptyState.Text), "Filtru neodpovídá žádný spis.");
                    builder.CloseComponent();
                }));
    }
}
