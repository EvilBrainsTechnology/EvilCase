using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.App.Components;

public partial class FilesCard
{
    private const string WaitForUpload = "Počkejte, až se dokončí nahrávání.";

    private int uploadIndex;

    private int uploadTotal;

    private string uploadName = "";

    private int uploadRound;

    /// <summary>
    /// The input whose batch is uploading.
    /// </summary>
    private int? busyRound;

    private InputFile? versionInput;

    private int versionRound;

    private FileListItem? versionTarget;

    private bool versionUploading;

    private async Task OnFilesSelected(InputFileChangeEventArgs args)
    {
        if (this.busyRound is not null || this.versionUploading)
        {
            if (args.FileCount == 0)
                return;

            if (!this.State.Failures.Contains(WaitForUpload))
                this.State.Failures.Add(WaitForUpload);

            this.uploadRound++;

            return;
        }

        this.busyRound = this.uploadRound++;
        this.State.UploadStarted();
        this.State.Failures.Clear();
        this.State.Notices.Clear();
        this.uploadIndex = 0;
        this.uploadTotal = args.FileCount;

        try
        {
            await FileBatchUploader.Run(
                args,
                this.UploadFile,
                this.OwnerGoneError,
                this.State.Failures,
                (index, name) =>
                {
                    this.uploadIndex = index;
                    this.uploadName = name;
                    this.StateHasChanged();
                },
                result => this.Record(result),
                this.Logger);
        }
        finally
        {
            this.busyRound = null;
            this.State.UploadEnded();
            this.uploadTotal = 0;
        }

        await this.ReloadFiles();
    }

    private async Task PickVersion(FileListItem file)
    {
        this.versionTarget = file;

        await this.BrowserFiles.PickFile(this.versionInput!.Element!.Value, CancellationToken.None);
    }

    private async Task OnVersionSelected(InputFileChangeEventArgs args)
    {
        if (args.FileCount == 0 || this.versionTarget is not { } file)
            return;

        this.versionRound++;

        var (failure, _) = await this.UploadVersion(file, args.File);

        if (failure is not null && !this.State.Failures.Contains(failure))
            this.State.Failures.Add(failure);
    }

    // Called from the modals through a plain delegate, so it redraws the card itself.
    private async Task<(string? Failure, string? Notice)> UploadVersion(FileListItem file, IBrowserFile picked)
    {
        if (this.busyRound is not null || this.versionUploading)
            return (WaitForUpload, null);

        if (FileVersionUploader.Refusal(file, picked) is { } refusal)
            return (refusal, null);

        this.versionUploading = true;
        this.State.UploadStarted();
        this.State.Failures.Clear();
        this.State.Notices.Clear();
        this.uploadIndex = 1;
        this.uploadTotal = 1;
        this.uploadName = picked.Name;
        this.StateHasChanged();

        string? failure;
        string? notice = null;

        try
        {
            (var result, failure) = await FileVersionUploader.Upload(this.UploadFileVersion, file, picked, this.Logger);

            if (result is not null)
                notice = this.Record(result);
        }
        finally
        {
            this.versionUploading = false;
            this.State.UploadEnded();
            this.uploadTotal = 0;
        }

        await this.ReloadFiles();

        this.StateHasChanged();

        return (failure, notice);
    }
}
