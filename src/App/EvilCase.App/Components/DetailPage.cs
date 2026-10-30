using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Clipboard;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Components;

/// <summary>
/// The load, label save, link copy, files card list, file content load and file version operations that the case and the act detail share.
/// </summary>
public abstract class DetailPage<TDetail> : ComponentBase
    where TDetail : class
{
    [Inject]
    protected NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private protected IFileTransferClient FileTransfer { get; set; } = null!;

    [Inject]
    private IFileVersionsClient FileVersions { get; set; } = null!;

    [Inject]
    private ILogger<DetailPage<TDetail>> Logger { get; set; } = null!;

    [Inject]
    private IClipboardWriter Clipboard { get; set; } = null!;

    private Task labelsWrite = Task.CompletedTask;

    private string? filesPath;

    protected TDetail? Detail { get; private set; }

    protected bool NotFound { get; private set; }

    protected string? Failure { get; private set; }

    protected string? LabelsError { get; private set; }

    protected string? Announcement { get; private set; }

    /// <summary>
    /// The files card's list, shared by the card's slot in each column.
    /// </summary>
    protected FilesCardState Files { get; private set; } = null!;

    protected abstract string LoadFailedText { get; }

    protected abstract string GoneText { get; }

    protected abstract string LinkCopiedText { get; }

    protected abstract string LinkPath { get; }

    protected abstract Task<TDetail> Fetch();

    protected abstract Task StoreLabels(TDetail detail, LabelAssignmentRequest request);

    protected abstract TDetail WithLabels(TDetail detail, IReadOnlyList<LabelItem> labels);

    protected abstract Task<IReadOnlyList<FileListItem>> LoadFiles(CancellationToken token);

    protected override async Task OnParametersSetAsync()
    {
        // The link path names the entity, so another case or act gets its own list.
        if (!string.Equals(this.filesPath, this.LinkPath, StringComparison.Ordinal))
        {
            this.filesPath = this.LinkPath;
            this.Files = new FilesCardState(this.LoadFiles);
        }

        await this.Load();
    }

    protected async Task Load()
    {
        this.Announcement = null;
        this.NotFound = false;
        this.Failure = null;
        this.LabelsError = null;
        this.Detail = null;

        try
        {
            this.Detail = await this.Fetch();
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            this.NotFound = true;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            this.Logger.LogWarning(exception, "Loading the detail failed");

            this.Failure = this.LoadFailedText;
        }
    }

    protected async Task SaveLabels(IReadOnlyList<LabelItem> labels)
    {
        // The page shows the change at once; the writes follow one after another, each to the detail it was made on.
        var detail = this.Detail!;
        var previous = this.labelsWrite;
        var written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        this.labelsWrite = written.Task;
        this.LabelsError = null;
        this.Detail = this.WithLabels(detail, labels);

        try
        {
            await previous;
            await this.StoreLabels(detail, new LabelAssignmentRequest { LabelIds = [.. labels.Select(static label => label.LabelId)] });
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            this.LabelsError = this.GoneText;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            this.Logger.LogWarning(exception, "Saving the labels failed");

            await this.Load();

            this.LabelsError = "Štítky se nepodařilo uložit. Zkuste to za chvíli znovu.";
        }
        finally
        {
            written.SetResult();
        }
    }

    protected async Task CopyLink()
    {
        var link = this.Navigation.ToAbsoluteUri(this.LinkPath).AbsoluteUri;

        try
        {
            await this.Clipboard.WriteText(link, CancellationToken.None);

            this.Announcement = this.LinkCopiedText;
        }
        catch (JSException exception)
        {
            this.Logger.LogWarning(exception, "Copying the link failed");
        }
    }

    protected async Task<Stream> LoadFileContent(FileListItem file, int? version, CancellationToken token)
    {
        return await this.FileTransfer.DownloadFileContent(file.FileId, version, token);
    }

    protected async Task<FileUploadResult> UploadFileVersion(FileListItem file, IBrowserFile picked, CancellationToken token)
    {
        return await this.FileTransfer.UploadFileVersion(file.FileId, picked, token);
    }

    protected async Task<IReadOnlyList<FileVersionItem>> LoadFileVersions(FileListItem file, CancellationToken token)
    {
        return (await this.FileVersions.ListFileVersions(file.FileId, token)).Items;
    }

    protected async Task DeleteFileVersion(FileListItem file, int number, CancellationToken token)
    {
        await this.FileVersions.DeleteFileVersion(file.FileId, number, token);
    }
}
