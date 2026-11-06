using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.App.Components;

public partial class FilePreviewModal
{
    private IReadOnlyList<FileVersionItem>? versions;

    private bool versionsFailed;

    private (Guid, int, int)? versionsKey;

    private bool versionsOpen;

    private string? versionFailure;

    private ElementReference versionsToggle;

    private InputFile? versionInput;

    private int versionRound;

    private bool focusVersionsToggle;

    // The older version Vrátit se k této verzi asks to restore.
    private int? restoring;

    private FileVersionItem? ShownVersionItem => this.OlderVersion is { } number ? this.versions?.FirstOrDefault(version => version.Number == number) : null;

    private async Task LoadVersionList(FileListItem file)
    {
        var key = (file.FileId, file.Version, file.VersionCount);

        if (file.VersionCount < 2)
        {
            this.versions = null;
            this.versionsKey = null;

            return;
        }

        if (key == this.versionsKey)
            return;

        this.versionsKey = key;
        this.versionsFailed = false;

        try
        {
            this.versions = await this.LoadVersions(file, CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            this.Logger.LogWarning(exception, "Loading the versions of file {FileAssetId} failed", file.FileId);

            this.versions = null;
            this.versionsFailed = true;
        }
    }

    private async Task RetryVersions(FileListItem file)
    {
        this.versionsKey = null;

        await this.LoadVersionList(file);
    }

    private void ToggleVersions()
    {
        if (this.OlderVersion is null)
            this.versionsOpen = !this.versionsOpen;
    }

    // The bar holding the focused button goes away, so the focus moves to the toggle once the current version shows.
    private async Task ShowCurrentVersion()
    {
        this.versionsOpen = false;
        this.focusVersionsToggle = true;

        await this.VersionChanged.InvokeAsync(null);
    }

    private async Task VersionRestored()
    {
        this.restoring = null;

        await this.ShowCurrentVersion();
    }

    private async Task SelectVersion(FileListItem file, int number)
    {
        this.versionsOpen = true;

        await this.VersionChanged.InvokeAsync(number == file.Version ? null : number);
    }

    private async Task UploadNewVersion(FileListItem file, IBrowserFile picked)
    {
        this.uploadingVersion = true;
        this.versionFailure = null;

        this.versionFailure = (await this.UploadVersion(file, picked)).Failure;

        this.uploadingVersion = false;

        if (this.versionFailure is null)
            await this.VersionChanged.InvokeAsync(null);
    }

    private async Task VersionPicked(FileListItem file, InputFileChangeEventArgs args)
    {
        if (args.FileCount == 0)
            return;

        // A fresh input, so the same file can be picked again.
        this.versionRound++;

        await this.UploadNewVersion(file, args.File);
    }

    private async Task PickVersion()
    {
        await this.BrowserFiles.PickFile(this.versionInput!.Element!.Value, CancellationToken.None);
    }

    private async Task DownloadVersion(FileListItem file, int number)
    {
        if (number == file.Version)
            await this.OnDownload.InvokeAsync(file);
        else
            await this.BrowserFiles.DownloadFile(file.FileId, number, file.FileName, CancellationToken.None);
    }

    private static bool Listed(IReadOnlyList<FileVersionItem> versions, int number)
    {
        return versions.Any(version => version.Number == number);
    }
}
