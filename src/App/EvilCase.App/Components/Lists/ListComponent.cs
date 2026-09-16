using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Search;
using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Components.Lists;

public abstract class ListComponent<TSortRequest> : ComponentBase, IDisposable
    where TSortRequest : ListRequest
{
    private readonly SearchDebouncer debouncer = new();

    private TSortRequest? loaded;

    [Inject]
    protected ILogger<ListComponent<TSortRequest>> Logger { get; set; } = null!;

    protected string SearchText { get; private set; } = "";

    protected ListSortDirection Direction { get; set; } = ListSortDirection.Ascending;

    protected int Skip { get; private set; }

    protected int Total { get; private set; }

    protected bool Loading { get; private set; } = true;

    protected string? Failure { get; private set; }

    protected string? Search => string.IsNullOrWhiteSpace(this.SearchText) ? null : this.SearchText;

    protected abstract int PageSize { get; }

    protected abstract string FailureText { get; }

    public void Dispose()
    {
        this.debouncer.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Reads the list again from its first page; the host calls it after it wrote a record.
    /// </summary>
    public async Task Reload()
    {
        await this.ReloadFirstPage();
    }

    /// <summary>
    /// Reads the page the toolbar asks for and answers with the count the filter leaves.
    /// </summary>
    protected abstract Task<int> LoadPage(CancellationToken token);

    protected async Task ReloadOnFilterChange(TSortRequest filter)
    {
        if (Equals(this.loaded, filter))
            return;

        this.loaded = filter;

        await this.ReloadFirstPage();
    }

    protected async Task ReloadFirstPage()
    {
        this.Skip = 0;

        await this.Load(debounce: false);
    }

    protected void TurnDirection()
    {
        this.Direction = this.Direction == ListSortDirection.Descending ? ListSortDirection.Ascending : ListSortDirection.Descending;
    }

    protected async Task OnSearchInput(ChangeEventArgs args)
    {
        this.SearchText = args.Value as string ?? "";
        this.Skip = 0;

        await this.Load(debounce: true);
    }

    protected async Task ToPreviousPage()
    {
        this.Skip = Math.Max(0, this.Skip - this.PageSize);

        await this.Load(debounce: false);
    }

    protected async Task ToNextPage()
    {
        this.Skip += this.PageSize;

        await this.Load(debounce: false);
    }

    private async Task Load(bool debounce)
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
