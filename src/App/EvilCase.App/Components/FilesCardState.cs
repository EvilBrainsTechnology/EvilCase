using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.App.Components;

/// <summary>
/// What a files card shows, kept apart from the card, so a card moved between the detail columns
/// shows the same list, highlights, notices and errors.
/// </summary>
public sealed class FilesCardState(Func<CancellationToken, Task<IReadOnlyList<FileListItem>>> loadFiles)
{
    private Task? firstLoad;

    public event EventHandler? Placed;

    public IReadOnlyList<FileListItem> Items { get; private set; } = [];

    public bool Loaded { get; private set; }

    public string? ListFailure { get; private set; }

    public List<string> Failures { get; } = [];

    public List<string> Notices { get; } = [];

    /// <summary>
    /// The files and versions stored since the page loaded.
    /// </summary>
    public Dictionary<Guid, FileUploadOutcome> Fresh { get; } = [];

    /// <summary>
    /// An upload runs: the card keeps its place until it ends.
    /// </summary>
    public bool Busy { get; set; }

    /// <summary>
    /// The card sits in the main column: the list holds a file.
    /// </summary>
    public bool Wide { get; private set; }

    public async Task EnsureLoaded(ILogger logger)
    {
        await (this.firstLoad ??= this.Reload(logger));
    }

    public async Task Reload(ILogger logger)
    {
        this.ListFailure = null;

        try
        {
            this.Items = await loadFiles(CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            logger.LogWarning(exception, "Loading the files failed");

            this.Items = [];
            this.ListFailure = "Soubory se nepodařilo načíst. Zkuste to za chvíli znovu.";
        }

        this.Loaded = true;
        this.Place();
    }

    /// <summary>
    /// Moves the card to the column its list asks for, unless an upload runs.
    /// </summary>
    public void Place()
    {
        var wide = this.Items.Count > 0;

        if (this.Busy || wide == this.Wide)
            return;

        this.Wide = wide;
        this.Placed?.Invoke(this, EventArgs.Empty);
    }
}
