using System.Diagnostics;
using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.App.Components;

/// <summary>
/// The files of an act and the operations on them, shared by the files card and the main-file preview.
/// Kept apart from the card, so a card moved between the detail columns shows the same list, highlights, notices and errors.
/// </summary>
public sealed class FilesCardState
{
    private readonly IActFilesClient actFiles;

    private readonly IFileTransferClient fileTransfer;

    private readonly IFileVersionsClient fileVersions;

    private readonly IMainFileClient mainFiles;

    private Task? firstLoad;

    private int uploads;

    internal FilesCardState(Guid actId, IActFilesClient actFiles, IFileTransferClient fileTransfer, IFileVersionsClient fileVersions, IMainFileClient mainFiles)
    {
        this.ActId = actId;
        this.actFiles = actFiles;
        this.fileTransfer = fileTransfer;
        this.fileVersions = fileVersions;
        this.mainFiles = mainFiles;
    }

    public event EventHandler? Placed;

    /// <summary>
    /// The list was loaded again and the card placed, unless an upload holds it.
    /// </summary>
    public event EventHandler? Changed;

    public Guid ActId { get; }

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
    /// An upload runs: the card keeps its place until every upload ends.
    /// </summary>
    public bool Busy => this.uploads > 0;

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
            this.Items = (await this.actFiles.ListActFiles(this.ActId, CancellationToken.None)).Items;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            logger.LogWarning(exception, "Loading the files failed");

            this.Items = [];
            this.ListFailure = "Soubory se nepodařilo načíst. Zkuste to za chvíli znovu.";
        }

        this.Loaded = true;
        this.Place();
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    public void UploadStarted()
    {
        this.uploads++;
    }

    public void UploadEnded()
    {
        this.uploads--;
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

    internal async Task<FileUploadResult> UploadFile(IBrowserFile file, CancellationToken token)
    {
        return await this.fileTransfer.UploadActFile(this.ActId, file, token);
    }

    internal async Task RenameFile(FileListItem file, string fileName, CancellationToken token)
    {
        await this.actFiles.RenameActFile(this.ActId, file.FileId, new FileRenameRequest { FileName = fileName }, token);
    }

    internal async Task DeleteFile(FileListItem file, CancellationToken token)
    {
        await this.actFiles.DeleteActFile(this.ActId, file.FileId, token);
    }

    /// <summary>
    /// The bytes the preview shows, of the numbered version or the current one.
    /// </summary>
    internal async Task<Stream> LoadFileContent(FileListItem file, int? version, CancellationToken token)
    {
        return await this.fileTransfer.DownloadFileContent(file.FileId, version, token);
    }

    internal async Task<FileUploadResult> UploadFileVersion(FileListItem file, IBrowserFile picked, CancellationToken token)
    {
        return await this.fileTransfer.UploadFileVersion(file.FileId, picked, token);
    }

    internal async Task<IReadOnlyList<FileVersionItem>> ListFileVersions(FileListItem file, CancellationToken token)
    {
        return (await this.fileVersions.ListFileVersions(file.FileId, token)).Items;
    }

    internal async Task DeleteFileVersion(FileListItem file, int number, CancellationToken token)
    {
        await this.fileVersions.DeleteFileVersion(file.FileId, number, token);
    }

    /// <summary>
    /// Stores the numbered version as the file's new version. It marks the file and prints its notice as an uploaded
    /// version does; one that stored nothing marks nothing.
    /// </summary>
    internal async Task RestoreFileVersion(FileListItem file, int number, CancellationToken token)
    {
        var restored = await this.fileVersions.RestoreFileVersion(file.FileId, number, token);
        var stored = restored.Version > file.Version;

        if (stored)
            this.Fresh.TryAdd(file.FileId, FileUploadOutcome.NewVersion);

        this.Failures.Clear();
        this.Notices.Clear();
        this.Notices.Add(stored ? string.Create(CultureInfo.InvariantCulture, $"{restored.FileName}: obnoveno jako verze {restored.Version}.") : UnchangedNotice(restored));
    }

    /// <summary>
    /// Sets the file as the main file of its act, or unsets it; answers the failure to show, or none.
    /// </summary>
    internal async Task<string?> ChangeMainFile(FileListItem file, bool main, ILogger logger)
    {
        try
        {
            if (main)
                await this.mainFiles.SetMainFile(file.FileId, CancellationToken.None);
            else
                await this.mainFiles.UnsetMainFile(file.FileId, CancellationToken.None);

            return null;
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning(exception, "Changing the main file failed: the file is gone");

            return $"Soubor {file.FileName} už neexistuje.";
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            logger.LogWarning(exception, "Changing the main file failed");

            return "Hlavní soubor se nepodařilo změnit. Zkuste to za chvíli znovu.";
        }
    }

    /// <summary>
    /// Marks what an upload stored and adds its notice; answers the notice, or none for a new file.
    /// </summary>
    internal string? Record(FileUploadResult result)
    {
        var file = result.File;

        // A file new since the page loaded stays new through its later versions.
        if (result.Outcome == FileUploadOutcome.NewFile)
            this.Fresh[file.FileId] = FileUploadOutcome.NewFile;
        else if (result.Outcome == FileUploadOutcome.NewVersion)
            this.Fresh.TryAdd(file.FileId, FileUploadOutcome.NewVersion);

        var notice = result.Outcome switch
        {
            FileUploadOutcome.NewFile => null,
            FileUploadOutcome.NewVersion => string.Create(CultureInfo.InvariantCulture, $"{file.FileName}: nahráno jako verze {file.Version}."),
            FileUploadOutcome.Unchanged => UnchangedNotice(file),
            _ => throw new UnreachableException(),
        };

        if (notice is not null)
            this.Notices.Add(notice);

        return notice;
    }

    private static string UnchangedNotice(FileListItem file)
    {
        return $"{file.FileName}: obsah je stejný jako poslední verze, nová verze nevznikla.";
    }
}
