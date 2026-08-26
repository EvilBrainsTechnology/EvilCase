using System.Net;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// SDD-012 pins the download's headers on the real pipeline, not on the controller alone:
/// <c>X-Content-Type-Options</c> comes from <c>SecurityHeadersMiddleware</c>, not from the action.
/// </summary>
public class FileDownloadHeaderTests
{
    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost(configureServices: services => services.AddSingleton<IFileReader>(new StubFileReader()));
        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [Test]
    public async Task TheDownloadIsAnAttachmentThatMustNotBeSniffed()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/api/files/{Guid.CreateVersion7()}/content", UriKind.Relative)) { Headers = { Authorization = TestTokens.BearerFrom(this.host) } };

        using var response = await this.client.SendAsync(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentDisposition?.DispositionType, Is.EqualTo("attachment"), "a stored document is never rendered in place");
            Assert.That(response.Content.Headers.ContentDisposition?.FileName, Is.EqualTo(StubFileReader.FileName));
            Assert.That(response.Headers.TryGetValues("X-Content-Type-Options", out var values) ? values.Single() : null, Is.EqualTo("nosniff"));
        }
    }

    private sealed class StubFileReader : IFileReader
    {
        public const string FileName = "smlouva.pdf";

        public Task<IReadOnlyList<FileListItem>?> ListCaseFiles(Guid caseId, CancellationToken token)
        {
            return Task.FromResult<IReadOnlyList<FileListItem>?>(null);
        }

        public Task<FileDownload?> OpenFileContent(Guid fileId, CancellationToken token)
        {
            return Task.FromResult<FileDownload?>(new FileDownload { FileName = FileName, MediaType = "application/pdf", Content = new MemoryStream("abc"u8.ToArray()) });
        }
    }
}
