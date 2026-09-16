using System.Globalization;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Search;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.App.Components.Lists;

/// <summary>
/// The toolbar state, the page and the load every list component shares.
/// </summary>
public abstract class ListComponent : ComponentBase, IDisposable
{
    private readonly SearchDebouncer debouncer = new();

    private ListRequest? loaded;

    [Inject]
    protected ILogger<ListComponent> Logger { get; set; } = null!;

    protected string SearchText { get; private set; } = "";

    protected DateOnly? From { get; private set; }

    protected DateOnly? To { get; private set; }

    protected IReadOnlyList<LabelItem> SelectedLabels { get; private set; } = [];

    protected ListSortDirection Direction { get; set; } = ListSortDirection.Descending;

    protected int Skip { get; private set; }

    protected int Total { get; private set; }

    protected bool Loading { get; private set; } = true;

    protected string? Failure { get; private set; }

    protected string? Search => string.IsNullOrWhiteSpace(this.SearchText) ? null : this.SearchText;

    protected IReadOnlyList<Guid> LabelIds => [.. this.SelectedLabels.Select(static label => label.LabelId)];

    protected abstract int PageSize { get; }

    protected abstract string FailureText { get; }

    public void Dispose()
    {
        this.debouncer.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Reads the page the toolbar asks for and answers with the count the filter leaves.
    /// </summary>
    protected abstract Task<int> LoadPage(CancellationToken token);

    /// <summary>
    /// Loads the first page again, unless the host's filter is the one already loaded.
    /// </summary>
    protected Task ReloadOnFilterChange(ListRequest filter)
    {
        if (this.loaded == filter)
            return Task.CompletedTask;

        this.loaded = filter;

        return this.ReloadFirstPage();
    }

    protected Task ReloadFirstPage()
    {
        this.Skip = 0;

        return this.Reload(debounce: false);
    }

    protected void TurnDirection()
    {
        this.Direction = this.Direction == ListSortDirection.Descending ? ListSortDirection.Ascending : ListSortDirection.Descending;
    }

    protected Task OnSearchInput(ChangeEventArgs args)
    {
        this.SearchText = args.Value as string ?? "";
        this.Skip = 0;

        return this.Reload(debounce: true);
    }

    protected Task OnFromInput(ChangeEventArgs args)
    {
        this.From = ParseDate(args);

        return this.ReloadFirstPage();
    }

    protected Task OnToInput(ChangeEventArgs args)
    {
        this.To = ParseDate(args);

        return this.ReloadFirstPage();
    }

    protected Task OnLabelsChanged(IReadOnlyList<LabelItem> labels)
    {
        this.SelectedLabels = labels;

        return this.ReloadFirstPage();
    }

    protected Task ToPreviousPage()
    {
        this.Skip = Math.Max(0, this.Skip - this.PageSize);

        return this.Reload(debounce: false);
    }

    protected Task ToNextPage()
    {
        this.Skip += this.PageSize;

        return this.Reload(debounce: false);
    }

    private static DateOnly? ParseDate(ChangeEventArgs args)
    {
        return DateOnly.TryParse(args.Value as string, CultureInfo.InvariantCulture, out var date) ? date : null;
    }

    private async Task Reload(bool debounce)
    {
        var token = await this.debouncer.Start(debounce);
        if (token is null)
            return;

        try
        {
            this.Total = await this.LoadPage(token.Value);
            this.Failure = null;
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer request.
            return;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            this.Logger.LogWarning(exception, "Loading a list failed");

            this.Failure = this.FailureText;
        }

        this.Loading = false;
        this.StateHasChanged();
    }
}
