using System.Net;
using System.Net.Http.Headers;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileTransferClientTests
{
    [Test]
    public async Task AnUploadPostsTheFileAsMultipartToTheCaseSFiles()
    {
        var caseId = Guid.CreateVersion7();
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.Created));

        var client = Client(handler);

        await client.UploadCaseFile(caseId, new StubBrowserFile("smlouva.pdf", "application/pdf", "abc"u8.ToArray()), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handler.Request!.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Request.RequestUri!.AbsolutePath, Is.EqualTo($"/api/cases/{caseId}/files"));
            Assert.That(handler.Request.Content!.Headers.ContentType!.MediaType, Is.EqualTo("multipart/form-data"));
            Assert.That(handler.Body, Does.Contain("name=file"));
            Assert.That(handler.Body, Does.Contain("smlouva.pdf"));
        }
    }

    [Test]
    public void AFailedUploadRaisesTheSameExceptionTheGeneratedClientsRaise()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge) { Content = new StringContent("too large") });
        var client = Client(handler);

        var exception = Assert.ThrowsAsync<ApiException>(
            async () => await client.UploadCaseFile(Guid.CreateVersion7(), new StubBrowserFile("a.txt", "text/plain", "a"u8.ToArray()), CancellationToken.None));

        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.RequestEntityTooLarge), "a failed upload raises the same exception type every generated client raises");
    }

    [Test]
    public async Task ADownloadReadsTheBytesAndTheMediaTypeOfTheResponse()
    {
        var bytes = "the content"u8.ToArray();
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var handler = new CapturingHandler(response);
        var client = Client(handler);

        var content = await client.DownloadFileContent(Guid.CreateVersion7(), CancellationToken.None);

        await using var reader = new MemoryStream();
        await content.Content.CopyToAsync(reader);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(content.MediaType, Is.EqualTo("application/pdf"));
            Assert.That(reader.ToArray(), Is.EqualTo(bytes));
        }
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
