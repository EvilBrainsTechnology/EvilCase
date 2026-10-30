using System.Net;
using System.Net.Http.Json;
using System.Text;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Business.Files.Links;
using EvilBrains.EvilCase.Domain.Files;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The link store and the file services are fakes; the anonymous endpoint, the tenant it enters and the answers run as in
/// production.
/// </summary>
public class FileLinkEndpointTests
{
    private const string Secret = "s3cr3t";

    private readonly Guid tenantId = Guid.CreateVersion7();

    private readonly Guid userId = Guid.CreateVersion7();

    private readonly Guid targetId = Guid.CreateVersion7();

    private readonly List<(Guid TenantId, Guid UserId, string FileName, string Content)> uploads = [];

    private ConsumedFileLink? link;

    private IFileLinkWriter links = null!;

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.links = Substitute.For<IFileLinkWriter>();
        this.links
            .ConsumeFileLink(Secret, Arg.Any<CancellationToken>())
            .Returns(_ => this.link is null
                ? new FileLinkConsumeResult { Outcome = FileLinkConsumeOutcome.NotFound }
                : new FileLinkConsumeResult { Outcome = FileLinkConsumeOutcome.Consumed, Link = this.link });

        this.host = new EvilCaseHost(configureServices: services =>
        {
            services.AddSingleton(this.links);

            // Records the user, tenant and content the upload runs with.
            services.AddScoped(provider =>
            {
                var userContext = provider.GetRequiredService<IUserContext>();
                var writer = Substitute.For<IFileWriter>();

                writer
                    .UploadCaseFile(this.targetId, Arg.Any<FileUpload>(), Arg.Any<CancellationToken>())
                    .Returns(async call =>
                    {
                        var upload = call.Arg<FileUpload>();
                        using var reader = new StreamReader(upload.Content);

                        this.uploads.Add((userContext.TenantId, userContext.UserId, upload.FileName, await reader.ReadToEndAsync()));

                        return new UploadFileResult { Outcome = UploadFileOutcome.Uploaded, File = File(upload.FileName) };
                    });

                return writer;
            });

            services.AddScoped(provider =>
            {
                var userContext = provider.GetRequiredService<IUserContext>();
                var reader = Substitute.For<IFileReader>();

                reader
                    .OpenFileContent(this.targetId, Arg.Any<int?>(), Arg.Any<CancellationToken>())
                    .Returns(_ => userContext.TenantIdOrDefault == this.tenantId
                        ? new FileDownload { FileName = "zapis.txt", MediaType = "text/plain", Content = new MemoryStream("obsah"u8.ToArray()) }
                        : null);

                return reader;
            });
        });

        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [SetUp]
    public void Forget()
    {
        this.links.ClearReceivedCalls();
        this.uploads.Clear();
        this.link = null;
    }

    [Test]
    public async Task AnUploadRunsAsTheUserAndTenantOfTheLink()
    {
        this.link = this.Link(FileLinkKind.CaseUpload);

        using var response = await this.Upload("zapis.txt", "obsah");
        var file = await response.Content.ReadFromJsonAsync<FileListItem>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(file?.FileName, Is.EqualTo("zapis.txt"), "the response returns the stored file");
            Assert.That(this.uploads, Is.EqualTo([(this.tenantId, this.userId, "zapis.txt", "obsah")]), "the anonymous upload runs as the link's user in the link's tenant, named by the link");
        }
    }

    [Test]
    public async Task AnUnknownOrSpentLinkIsNotFound()
    {
        using var response = await this.Upload("zapis.txt", "obsah");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(this.uploads, Is.Empty, "nothing is stored without a live link");
        }
    }

    [Test]
    public async Task ADownloadLinkDoesNotUpload()
    {
        this.link = this.Link(FileLinkKind.Download);

        using var response = await this.Upload("zapis.txt", "obsah");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "a link does only what it was created for");
            Assert.That(this.uploads, Is.Empty);
        }
    }

    [Test]
    public async Task AnInvalidNameIsRefusedBeforeTheLinkIsSpent()
    {
        this.link = this.Link(FileLinkKind.CaseUpload);

        using var response = await this.Upload(" ", "obsah");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "the upload checks the name as the multipart upload does");
            await this.links.DidNotReceive().ConsumeFileLink(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task ADownloadSendsTheContentUncached()
    {
        this.link = this.Link(FileLinkKind.Download);

        using var response = await this.client.GetAsync(new Uri($"{FileLinkRoute.Path}?{FileLinkRoute.SecretParameter}={Secret}", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the download runs in the link's tenant");
            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("obsah"));
            Assert.That(response.Headers.CacheControl?.NoStore, Is.True, "a linked download is never cached");
        }
    }

    [Test]
    public async Task AnUploadLinkDoesNotDownload()
    {
        this.link = this.Link(FileLinkKind.CaseUpload);

        using var response = await this.client.GetAsync(new Uri($"{FileLinkRoute.Path}?{FileLinkRoute.SecretParameter}={Secret}", UriKind.Relative));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "a link does only what it was created for");
    }

    private static FileListItem File(string fileName)
    {
        return new FileListItem
        {
            FileId = Guid.CreateVersion7(),
            FileName = fileName,
            SizeBytes = 5,
            Created = DateTime.UtcNow,
            Version = 1,
            VersionCount = 1,
        };
    }

    private ConsumedFileLink Link(FileLinkKind kind)
    {
        return new ConsumedFileLink
        {
            TenantId = this.tenantId,
            UserId = this.userId,
            Kind = kind,
            TargetId = this.targetId,
        };
    }

    private async Task<HttpResponseMessage> Upload(string fileName, string content)
    {
        var path = $"{FileLinkRoute.Path}?{FileLinkRoute.SecretParameter}={Secret}&{FileLinkRoute.FileNameParameter}={Uri.EscapeDataString(fileName)}";

        using var body = new StringContent(content, Encoding.UTF8, "text/plain");

        return await this.client.PutAsync(new Uri(path, UriKind.Relative), body);
    }
}
