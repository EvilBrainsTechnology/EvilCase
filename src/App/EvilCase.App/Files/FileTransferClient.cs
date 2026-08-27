using System.Net.Http.Headers;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class FileTransferClient(HttpClient httpClient) : IFileTransferClient
{
    private const string DefaultMediaType = "application/octet-stream";

    public Task UploadCaseFile(Guid caseId, IBrowserFile file, CancellationToken token)
    {
        return this.Upload(new Uri($"api/cases/{caseId}/files", UriKind.Relative), file, token);
    }

    public Task UploadActFile(Guid caseId, Guid actId, IBrowserFile file, CancellationToken token)
    {
        return this.Upload(new Uri($"api/cases/{caseId}/acts/{actId}/files", UriKind.Relative), file, token);
    }

    private async Task Upload(Uri route, IBrowserFile file, CancellationToken token)
    {
        await using var stream = file.OpenReadStream(FileLimits.MaxUploadBytes, token);

        using var content = new MultipartFormDataContent();
        using var part = new StreamContent(stream);
        part.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(file.ContentType) ? DefaultMediaType : file.ContentType);
        content.Add(part, "file", file.Name);

        using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = content };
        using var response = await httpClient.SendAsync(request, token);
        await EnsureSuccess(response, token);
    }

    public async Task<FileContent> DownloadFileContent(Guid fileId, CancellationToken token)
    {
        using var response = await httpClient.GetAsync(new Uri($"api/files/{fileId}/content", UriKind.Relative), token);
        await EnsureSuccess(response, token);

        var bytes = await response.Content.ReadAsByteArrayAsync(token);

        return new FileContent
        {
            MediaType = response.Content.Headers.ContentType?.MediaType ?? DefaultMediaType,
            Content = new MemoryStream(bytes),
        };
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
