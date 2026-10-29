using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class FileTransferClient(HttpClient httpClient) : IFileTransferClient
{
    private const string DefaultMediaType = "application/octet-stream";

    public async Task<FileUploadResult> UploadCaseFile(Guid caseId, IBrowserFile file, CancellationToken token)
    {
        return await this.Upload(new Uri($"api/cases/{caseId}/files", UriKind.Relative), file, token);
    }

    public async Task<FileUploadResult> UploadActFile(Guid actId, IBrowserFile file, CancellationToken token)
    {
        return await this.Upload(new Uri($"api/acts/{actId}/files", UriKind.Relative), file, token);
    }

    public async Task<FileUploadResult> UploadFileVersion(Guid fileId, IBrowserFile file, CancellationToken token)
    {
        return await this.Upload(new Uri($"api/files/{fileId}/versions", UriKind.Relative), file, token);
    }

    private async Task<FileUploadResult> Upload(Uri route, IBrowserFile file, CancellationToken token)
    {
        await using var stream = file.OpenReadStream(FileLimits.MaxUploadBytes, token);

        using var content = new MultipartFormDataContent();
        using var part = new StreamContent(stream);
        part.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(file.ContentType) ? DefaultMediaType : file.ContentType);
        content.Add(part, "file", file.Name);

        using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = content };
        using var response = await httpClient.SendAsync(request, token);
        await EnsureSuccess(response, token);

        var stored = await response.Content.ReadFromJsonAsync<FileListItem>(JsonSerializerOptions.Web, token)
            ?? throw new ApiException(response.StatusCode, responseBody: null);

        // 201 stores a file or a version, 200 stores nothing; a stored file at version 1 is new.
        return new FileUploadResult { File = stored, Outcome = Outcome(response.StatusCode, stored) };
    }

    public async Task<Stream> DownloadFileContent(Guid fileId, int? version, CancellationToken token)
    {
        using var response = await httpClient.GetAsync(new Uri(FileUrl.Content(fileId, version), UriKind.Relative), token);
        await EnsureSuccess(response, token);

        return new MemoryStream(await response.Content.ReadAsByteArrayAsync(token));
    }

    private static FileUploadOutcome Outcome(HttpStatusCode status, FileListItem stored)
    {
        if (status != HttpStatusCode.Created)
            return FileUploadOutcome.Unchanged;

        return stored.Version == 1 ? FileUploadOutcome.NewFile : FileUploadOutcome.NewVersion;
    }

    // The same failure the generated clients raise, so every screen catches one exception type.
    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken token)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(token);

        throw new ApiException(response.StatusCode, body.Length == 0 ? null : body);
    }
}
