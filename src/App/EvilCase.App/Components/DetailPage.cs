using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Clipboard;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Components;

/// <summary>
/// The load, label save, link copy and file download that the case and the act detail share.
/// </summary>
public abstract class DetailPage<TDetail> : ComponentBase
    where TDetail : class
{
    [Inject]
    protected NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ILogger<DetailPage<TDetail>> Logger { get; set; } = null!;

    [Inject]
    private IClipboardWriter Clipboard { get; set; } = null!;

    [Inject]
    private IFileTransferClient FileTransfer { get; set; } = null!;

    [Inject]
    private IFileDownloader Downloader { get; set; } = null!;

    protected TDetail? Detail { get; private set; }

    protected bool NotFound { get; private set; }

    protected string? Failure { get; private set; }

    protected string? LabelsError { get; private set; }

    protected string? Announcement { get; private set; }

    protected abstract string LoadFailedText { get; }

    protected abstract string GoneText { get; }

    protected abstract string LinkCopiedText { get; }

    protected abstract string LinkPath { get; }

    protected abstract Task<TDetail> Fetch();

    protected abstract Task StoreLabels(LabelAssignmentRequest request);

    protected override async Task OnParametersSetAsync()
    {
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
        this.LabelsError = null;

        try
        {
            await this.StoreLabels(new LabelAssignmentRequest { LabelIds = [.. labels.Select(static label => label.LabelId)] });

            this.Detail = await this.Fetch();
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            this.LabelsError = this.GoneText;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            this.Logger.LogWarning(exception, "Saving the labels failed");

            this.LabelsError = "Štítky se nepodařilo uložit. Zkuste to za chvíli znovu.";
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

    protected async Task DownloadFile(FileListItem file, CancellationToken token)
    {
        var content = await this.FileTransfer.DownloadFileContent(file.FileId, token);
        await this.Downloader.SaveFile(file.FileName, content, token);
    }
}
