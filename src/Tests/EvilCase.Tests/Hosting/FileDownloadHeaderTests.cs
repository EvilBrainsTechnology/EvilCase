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
        [Guid.CreateVersion7()] = ("skript.js", "text/javascript"),
        [Guid.CreateVersion7()] = ("Příloha č. 1 – \"foto\".png", "image/png"),
        [Guid.CreateVersion7()] = ("Účtenka přestupku.zip", "application/zip"),
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

    [TestCase("archiv.zip")]
    [TestCase("schema.svg")]
    [TestCase("stranka.html")]
    [TestCase("skript.js")]
    [TestCase("smlouva.pdf")]
    public async Task AnyOtherFileIsAnOctetStreamAttachment(string fileName)
    {
        using var response = await this.Get(fileName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentDisposition?.DispositionType, Is.EqualTo("attachment"), "a stored document is never rendered in place");
            Assert.That(response.Content.Headers.ContentDisposition?.FileName, Is.EqualTo(fileName));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/octet-stream"), "the stored type is never served for an attachment");
            Assert.That(Header(response, "X-Content-Type-Options"), Is.EqualTo("nosniff"));
        }
    }

    [TestCase("Příloha č. 1 – \"foto\".png", "inline")]
    [TestCase("Účtenka přestupku.zip", "attachment")]
    public async Task ANonAsciiNameTravelsAsUtf8WithAnAsciiFallback(string fileName, string dispositionType)
    {
        using var response = await this.Get(fileName);

        var disposition = response.Content.Headers.ContentDisposition;
        var raw = response.Content.Headers.GetValues("Content-Disposition").Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(disposition?.DispositionType, Is.EqualTo(dispositionType));
            Assert.That(disposition?.FileNameStar, Is.EqualTo(fileName), "filename* carries the name in UTF-8 (RFC 6266, RFC 5987)");
            Assert.That(disposition?.FileName, Is.Not.Empty, "a client without filename* support still gets a name");
            Assert.That(raw.All(char.IsAscii), Is.True, "a header value is ASCII");
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
    public async Task TheContentIsSandboxedAndFramedByNobody()
    {
        using var response = await this.Get("foto.png");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Header(response, "X-Frame-Options"), Is.EqualTo("DENY"));
            Assert.That(Header(response, "Content-Security-Policy"), Is.EqualTo("sandbox; frame-ancestors 'none'"), "no inline file runs script on the app's origin");
        }
    }

    [TestCase("/api/files/{0}/content/")]
    [TestCase("/API/Files/{0}/CONTENT")]
    public async Task EveryPathTheRouteAcceptsIsSandboxed(string pathFormat)
    {
        var fileId = Files.Single(static file => string.Equals(file.Value.FileName, "foto.png", StringComparison.Ordinal)).Key;

        using var response = await this.Send(string.Format(CultureInfo.InvariantCulture, pathFormat, fileId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Content.Headers.ContentDisposition?.DispositionType, Is.EqualTo("inline"));
            Assert.That(Header(response, "Content-Security-Policy"), Is.EqualTo("sandbox; frame-ancestors 'none'"), "an inline file served on any path the route accepts is sandboxed");
        }
    }

    [TestCase("smlouva.pdf")]
    [TestCase("archiv.zip")]
    public async Task NoCacheStoresTheContent(string fileName)
    {
        using var response = await this.Get(fileName);

        Assert.That(response.Headers.CacheControl?.NoStore, Is.True, "an authenticated file never stays in a browser or shared cache");
    }

    private static string? Header(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values) ? values.Single() : null;
    }

    private async Task<HttpResponseMessage> Get(string fileName, RangeHeaderValue? range = null)
    {
        var fileId = Files.Single(file => string.Equals(file.Value.FileName, fileName, StringComparison.Ordinal)).Key;

        return await this.Send($"/api/files/{fileId}/content", range);
    }

    private async Task<HttpResponseMessage> Send(string path, RangeHeaderValue? range = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative)) { Headers = { Authorization = TestTokens.BearerFrom(this.host), Range = range } };

        return await this.client.SendAsync(request);
    }
}
