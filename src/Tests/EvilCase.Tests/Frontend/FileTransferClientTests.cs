using System.Net;
using System.Net.Http.Json;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileTransferClientTests
{
    [Test]
    public async Task AnUploadPostsTheFileAsMultipartToTheCaseSFiles()
    {
        var caseId = Guid.CreateVersion7();
        var handler = new CapturingHandler(Stored(HttpStatusCode.Created, 1, 1));

        var client = Client(handler);

        var result = await client.UploadCaseFile(caseId, new StubBrowserFile("smlouva.pdf", "application/pdf", "abc"u8.ToArray()), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handler.Request!.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Request.RequestUri!.AbsolutePath, Is.EqualTo($"/api/cases/{caseId}/files"));
            Assert.That(handler.Request.Content!.Headers.ContentType!.MediaType, Is.EqualTo("multipart/form-data"));
            Assert.That(handler.Body, Does.Contain("name=file"));
            Assert.That(handler.Body, Does.Contain("smlouva.pdf"));
            Assert.That(result.Outcome, Is.EqualTo(FileUploadOutcome.NewFile));
        }
    }

    [Test]
    public async Task AnUploadThatAddedAVersionSaysSo()
    {
        var client = Client(new CapturingHandler(Stored(HttpStatusCode.Created, 3, 3)));

        var result = await client.UploadCaseFile(Guid.CreateVersion7(), new StubBrowserFile("a.txt", "text/plain", "a"u8.ToArray()), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(FileUploadOutcome.NewVersion));
            Assert.That(result.File.Version, Is.EqualTo(3));
        }
    }

    [Test]
    public async Task AnUploadThatStoredNothingSaysSo()
    {
        var client = Client(new CapturingHandler(Stored(HttpStatusCode.OK, 2, 2)));

        var result = await client.UploadActFile(Guid.CreateVersion7(), new StubBrowserFile("a.txt", "text/plain", "a"u8.ToArray()), CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(FileUploadOutcome.Unchanged));
    }

    [Test]
    public async Task ANewVersionPostsTheFileToTheVersionsOfTheFile()
    {
        var fileId = Guid.CreateVersion7();
        var handler = new CapturingHandler(Stored(HttpStatusCode.Created, 2, 2));
        var client = Client(handler);

        await client.UploadFileVersion(fileId, new StubBrowserFile("a.txt", "text/plain", "a"u8.ToArray()), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handler.Request!.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Request.RequestUri!.AbsolutePath, Is.EqualTo($"/api/files/{fileId}/versions"));
            Assert.That(handler.Request.Content!.Headers.ContentType!.MediaType, Is.EqualTo("multipart/form-data"));
            Assert.That(handler.Body, Does.Contain("name=file"));
        }
    }

    [Test]
    public async Task ADownloadOfAVersionNamesItsNumber()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent("x"u8.ToArray()) });
        var client = Client(handler);
        var fileId = Guid.CreateVersion7();

        await using var content = await client.DownloadFileContent(fileId, version: 2, CancellationToken.None);

        Assert.That(handler.Request?.RequestUri?.PathAndQuery, Is.EqualTo($"/api/files/{fileId}/content?version=2"));
    }

    [Test]
    public async Task AFailedUploadRaisesTheSameExceptionTheGeneratedClientsRaise()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge) { Content = new StringContent("too large") });
        var client = Client(handler);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await client.UploadCaseFile(Guid.CreateVersion7(), new StubBrowserFile("a.txt", "text/plain", "a"u8.ToArray()), CancellationToken.None));

        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), "a failed upload raises the same exception type every generated client raises");
    }

    [Test]
    public async Task ADownloadReadsTheBytesFromTheContentAddress()
    {
        var bytes = "the content"u8.ToArray();
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };

        var handler = new CapturingHandler(response);
        var client = Client(handler);

        var fileId = Guid.CreateVersion7();
        await using var content = await client.DownloadFileContent(fileId, version: null, CancellationToken.None);

        await using var reader = new MemoryStream();
        await content.CopyToAsync(reader);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handler.Request?.RequestUri?.AbsolutePath, Is.EqualTo($"/api/files/{fileId}/content"));
            Assert.That(reader.ToArray(), Is.EqualTo(bytes));
        }
    }

    private static HttpResponseMessage Stored(HttpStatusCode status, int version, int versionCount)
    {
        var stored = new FileListItem { FileId = Guid.CreateVersion7(), FileName = "a.txt", SizeBytes = 1, Created = DateTime.UtcNow, Version = version, VersionCount = versionCount };

        return new HttpResponseMessage(status) { Content = JsonContent.Create(stored) };
    }

    private static FileTransferClient Client(HttpMessageHandler handler)
    {
        return new FileTransferClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") });
    }

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Request = request;
            this.Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);

            return response;
        }
    }

    private sealed class StubBrowserFile(string name, string contentType, byte[] content) : IBrowserFile
    {
        public string Name { get; } = name;

        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

        public long Size { get; } = content.Length;

        public string ContentType { get; } = contentType;

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            return new MemoryStream(content);
        }
    }
}
