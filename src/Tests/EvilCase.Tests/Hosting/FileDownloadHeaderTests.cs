using System.Net;
using System.Net.Http.Headers;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// <c>X-Content-Type-Options</c> and the frame headers come from <c>SecurityHeadersMiddleware</c>, not the
/// action, so only the real pipeline shows them.
/// </summary>
public class FileDownloadHeaderTests
{
    private static readonly Dictionary<Guid, (string FileName, string StoredMediaType)> Files = new()
    {
        [Guid.CreateVersion7()] = ("smlouva.pdf", "application/octet-stream"),
        [Guid.CreateVersion7()] = ("foto.png", "text/html"),
        [Guid.CreateVersion7()] = ("zaznam.mp4", "application/octet-stream"),
        [Guid.CreateVersion7()] = ("archiv.zip", "application/zip"),
        [Guid.CreateVersion7()] = ("schema.svg", "image/svg+xml"),
        [Guid.CreateVersion7()] = ("stranka.html", "text/html"),
    };

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost(configureServices: static services =>
        {
            var reader = Substitute.For<IFileReader>();
            reader
                .OpenFileContent(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(static call =>
                {
                    var (fileName, mediaType) = Files[call.Arg<Guid>()];

                    return new FileDownload { FileName = fileName, MediaType = mediaType, Content = new MemoryStream("0123456789"u8.ToArray()) };
                });
            services.AddSingleton(reader);
        });
        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [TestCase("smlouva.pdf", "application/pdf")]
    [TestCase("foto.png", "image/png")]
    [TestCase("zaznam.mp4", "video/mp4")]
    public async Task AFileTheBrowserShowsNativelyIsInlineUnderTheDetectedType(string fileName, string mediaType)
    {
        using var response = await this.Get(fileName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentDisposition?.DispositionType, Is.EqualTo("inline"));
            Assert.That(response.Content.Headers.ContentDisposition?.FileName, Is.EqualTo(fileName));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(mediaType), "the stored type is never used for an inline file");
            Assert.That(Header(response, "X-Content-Type-Options"), Is.EqualTo("nosniff"));
        }
    }

    [TestCase("archiv.zip", "application/zip")]
    [TestCase("schema.svg", "image/svg+xml")]
    [TestCase("stranka.html", "text/html")]
    public async Task AnyOtherFileIsAnAttachmentUnderTheStoredType(string fileName, string mediaType)
    {
        using var response = await this.Get(fileName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentDisposition?.DispositionType, Is.EqualTo("attachment"), "a stored document is never rendered in place");
            Assert.That(response.Content.Headers.ContentDisposition?.FileName, Is.EqualTo(fileName));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo(mediaType));
            Assert.That(Header(response, "X-Content-Type-Options"), Is.EqualTo("nosniff"));
        }
    }

    [Test]
    public async Task ARangeRequestAnswersWithThatPartOfTheFile()
    {
        using var response = await this.Get("zaznam.mp4", new RangeHeaderValue(2, 5));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PartialContent));
            Assert.That(response.Content.Headers.ContentRange?.ToString(), Is.EqualTo("bytes 2-5/10"));
            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("2345"));
        }
    }

    [Test]
    public async Task TheContentIsFrameableByTheAppItself()
    {
        using var response = await this.Get("smlouva.pdf");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Header(response, "X-Frame-Options"), Is.EqualTo("SAMEORIGIN"));
            Assert.That(Header(response, "Content-Security-Policy"), Does.Contain("frame-ancestors 'self'").And.Not.Contain("frame-ancestors 'none'"));
        }
    }

    private static string? Header(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values) ? values.Single() : null;
    }

    private async Task<HttpResponseMessage> Get(string fileName, RangeHeaderValue? range = null)
    {
        var fileId = Files.Single(file => string.Equals(file.Value.FileName, fileName, StringComparison.Ordinal)).Key;

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/api/files/{fileId}/content", UriKind.Relative)) { Headers = { Authorization = TestTokens.BearerFrom(this.host), Range = range } };

        return await this.client.SendAsync(request);
    }
}
