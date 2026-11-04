using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// Uploads a picked file as a file's new version and turns a refusal or a failure into a Czech message.
/// </summary>
internal static class FileVersionUploader
{
    public static string TooLarge(string name)
    {
        return $"{name}: soubor je větší než {FileSizeDisplay.Text(FileLimits.MaxUploadBytes)}.";
    }

    /// <summary>
    /// Why the picked file cannot become the file's new version, decided before anything is sent.
    /// </summary>
    public static string? Refusal(FileListItem file, IBrowserFile picked)
    {
        if (!FileNameRules.SameExtension(file.FileName, picked.Name))
        {
            var extension = Path.GetExtension(file.FileName);

            return $"{picked.Name}: nová verze souboru {file.FileName} musí {(extension.Length == 0 ? "být bez přípony" : $"mít příponu {extension}")}.";
        }

        return picked.Size > FileLimits.MaxUploadBytes ? TooLarge(picked.Name) : null;
    }

    public static async Task<(FileUploadResult? Result, string? Failure)> Upload(
        Func<FileListItem, IBrowserFile, CancellationToken, Task<FileUploadResult>> uploadVersion,
        FileListItem file,
        IBrowserFile picked,
        ILogger logger)
    {
        try
        {
            return (await uploadVersion(file, picked, CancellationToken.None), null);
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.RequestEntityTooLarge)
        {
            logger.LogWarning(exception, "Uploading a file version failed: too large");

            return (null, TooLarge(picked.Name));
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning(exception, "Uploading a file version failed: the file is gone");

            return (null, $"{picked.Name}: soubor {file.FileName} už neexistuje.");
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or IOException or JSException)
        {
            logger.LogWarning(exception, "Uploading a file version failed");

            return (null, $"{picked.Name}: nahrání se nezdařilo.");
        }
    }
}
