using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The upload loop shared by the files card's card-wide drop and the upload modal's picker:
/// enforces the batch and per-file size limits and turns a failure into a Czech message.
/// </summary>
internal static class FileBatchUploader
{
    public const int MaxBatchFiles = 100;

    public static async Task<bool> Run(
        InputFileChangeEventArgs args,
        Func<IBrowserFile, CancellationToken, Task> uploadFile,
        string ownerGoneError,
        List<string> failures,
        Action<int, string> reportProgress,
        ILogger logger)
    {
        if (args.FileCount > MaxBatchFiles)
        {
            failures.Add($"Najednou lze nahrát nejvýše {MaxBatchFiles} souborů.");

            return false;
        }

        var uploadedAny = false;
        var index = 0;

        foreach (var file in args.GetMultipleFiles(MaxBatchFiles))
        {
            index++;
            reportProgress(index, file.Name);

            if (file.Size > FileLimits.MaxUploadBytes)
            {
                failures.Add($"{file.Name}: soubor je větší než {FileSizeDisplay.Text(FileLimits.MaxUploadBytes)}.");
                continue;
            }

            try
            {
                await uploadFile(file, CancellationToken.None);

                uploadedAny = true;
            }
            catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                logger.LogWarning(exception, "Uploading a file failed: too large");

                failures.Add($"{file.Name}: soubor je větší než {FileSizeDisplay.Text(FileLimits.MaxUploadBytes)}.");
            }
            catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning(exception, "Uploading a file failed: the owner is gone");

                failures.Add($"{file.Name}: {ownerGoneError}");
            }
            catch (Exception exception) when (exception is ApiException or HttpRequestException or IOException or JSException)
            {
                logger.LogWarning(exception, "Uploading a file failed");

                failures.Add($"{file.Name}: nahrání se nezdařilo.");
            }
        }

        return uploadedAny;
    }
}
