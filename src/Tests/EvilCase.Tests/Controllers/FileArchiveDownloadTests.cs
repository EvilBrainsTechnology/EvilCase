using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class FileArchiveDownloadTests
{
    [Test]
    public async Task AnArchiveIsAZipAttachmentUnderItsCzechName()
    {
        var caseId = Guid.CreateVersion7();
        var archives = Substitute.For<IFileArchiveReader>();
        archives.ReadCaseFileArchive(caseId, includeVersions: true, Arg.Any<CancellationToken>()).Returns(Archive("Přestupek Novák (včetně verzí).zip"));
        archives
            .WriteFileArchive(Arg.Any<FileArchive>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(static async call => await call.Arg<Stream>().WriteAsync("PK"u8.ToArray()));

        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;

        var result = await Controller(context).DownloadCaseFileArchive(archives, NullLogger<FileTransferController>.Instance, caseId, includeVersions: true, CancellationToken.None);

        var disposition = ContentDispositionHeaderValue.Parse(context.Response.Headers.ContentDisposition.ToString());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.InstanceOf<EmptyResult>(), "the action has written the body itself");
            Assert.That(context.Response.ContentType, Is.EqualTo("application/zip"));
            Assert.That(disposition.DispositionType.Value, Is.EqualTo("attachment"));
            Assert.That(disposition.FileNameStar.Value, Is.EqualTo("Přestupek Novák (včetně verzí).zip"), "the Czech name travels as RFC 5987");
            Assert.That(body.ToArray(), Is.EqualTo("PK"u8.ToArray()));
        }
    }

    [Test]
    public async Task AnUnknownOwnerIsAProblemWithFourOhFour()
    {
        var archives = Substitute.For<IFileArchiveReader>();
        var controller = Controller(new DefaultHttpContext());

        var caseResult = await controller.DownloadCaseFileArchive(archives, NullLogger<FileTransferController>.Instance, Guid.CreateVersion7(), includeVersions: false, CancellationToken.None);
        var actResult = await controller.DownloadActFileArchive(archives, NullLogger<FileTransferController>.Instance, Guid.CreateVersion7(), includeVersions: false, CancellationToken.None);

        AssertProblem(caseResult, 404);
        AssertProblem(actResult, 404);
        await archives
            .DidNotReceiveWithAnyArgs()
            .WriteFileArchive(default!, default!, default);
    }

    [Test]
    public async Task AFailureAfterTheFirstByteAbortsTheResponse()
    {
        var actId = Guid.CreateVersion7();
        var archives = FailingArchives(actId);
        var lifetime = new RecordingLifetime();

        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponse());
        context.Features.Set<IHttpRequestLifetimeFeature>(lifetime);

        var result = await Controller(context).DownloadActFileArchive(archives, NullLogger<FileTransferController>.Instance, actId, includeVersions: false, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lifetime.Aborted, Is.True, "a cut archive must fail the download, never end as if it were whole");
            Assert.That(result, Is.InstanceOf<EmptyResult>());
        }
    }

    [Test]
    public async Task AFailureBeforeTheFirstByteIsAnOrdinaryError()
    {
        var actId = Guid.CreateVersion7();
        var archives = FailingArchives(actId);
        var lifetime = new RecordingLifetime();

        var context = new DefaultHttpContext();
        context.Features.Set<IHttpRequestLifetimeFeature>(lifetime);

        await Assert.ThrowsAsync<IOException>(async () => await Controller(context).DownloadActFileArchive(archives, NullLogger<FileTransferController>.Instance, actId, includeVersions: false, CancellationToken.None));
        Assert.That(lifetime.Aborted, Is.False, "nothing was sent, so the error handler still answers");
    }

    private static FileTransferController Controller(HttpContext context)
    {
        return new FileTransferController { ControllerContext = new ControllerContext { HttpContext = context } };
    }

    private static IFileArchiveReader FailingArchives(Guid actId)
    {
        var archives = Substitute.For<IFileArchiveReader>();
        archives.ReadActFileArchive(actId, includeVersions: false, Arg.Any<CancellationToken>()).Returns(Archive("Odvolání.zip"));
        archives
            .WriteFileArchive(Arg.Any<FileArchive>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException(new IOException("The blob is missing.")));

        return archives;
    }

    private static FileArchive Archive(string fileName)
    {
        return new FileArchive { OwnerId = Guid.CreateVersion7(), FileName = fileName, Entries = [] };
    }

    private sealed class StartedResponse : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }

    private sealed class RecordingLifetime : IHttpRequestLifetimeFeature
    {
        public bool Aborted { get; private set; }

        public CancellationToken RequestAborted { get; set; }

        public void Abort()
        {
            this.Aborted = true;
        }
    }
}
