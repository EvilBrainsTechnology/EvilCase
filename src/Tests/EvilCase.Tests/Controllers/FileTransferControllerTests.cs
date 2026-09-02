using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class FileTransferControllerTests
{
    [Test]
    public async Task AnUploadOverTheLimitIsRefusedWithFourThirteen()
    {
        var writer = Substitute.For<IFileWriter>();
        var controller = new FileTransferController();
        var file = FormFile(FileLimits.MaxUploadBytes + 1);

        var response = await controller.UploadCaseFile(writer, Guid.CreateVersion7(), file, CancellationToken.None);

        AssertProblem(response.Result, 413);
        Assert.That(writer.ReceivedCalls(), Is.Empty, "an upload over the limit must never reach the writer");
    }

    [Test]
    public async Task AnUploadAtTheLimitReachesTheWriter()
    {
        var writer = UploadingWriter(Uploaded(Item()));
        var controller = new FileTransferController();
        var file = FormFile(FileLimits.MaxUploadBytes);

        await controller.UploadCaseFile(writer, Guid.CreateVersion7(), file, CancellationToken.None);

        await writer
            .Received(1)
            .UploadCaseFile(Arg.Any<Guid>(), Arg.Any<FileUpload>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AnUploadKeepsOnlyTheNameItArrivedUnder()
    {
        FileUpload? upload = null;
        var writer = UploadingWriter(Uploaded(Item()), captured => upload = captured);
        var controller = new FileTransferController();
        var file = FormFile(1, fileName: "../../evil.txt");

        await controller.UploadCaseFile(writer, Guid.CreateVersion7(), file, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload?.FileName, Is.EqualTo("evil.txt"), "an upload keeps only the name it arrived under, not the path");
            Assert.That(upload?.MediaType, Is.EqualTo("application/pdf"));
        }
    }

    [Test]
    public async Task AnUploadWithAFileNameOverTheColumnLimitIsRefusedWithFourHundred()
    {
        var writer = Substitute.For<IFileWriter>();
        var controller = new FileTransferController();
        var file = FormFile(1, fileName: new string('a', FileLimits.MaxFileNameLength + 1) + ".pdf");

        var response = await controller.UploadCaseFile(writer, Guid.CreateVersion7(), file, CancellationToken.None);

        AssertProblem(response.Result, 400);
        Assert.That(writer.ReceivedCalls(), Is.Empty, "a file name over the FileAsset.FileName column limit must never reach the writer");
    }

    [Test]
    public async Task AnUploadWithAnEmptyFileNameIsRefusedWithFourHundred()
    {
        var writer = Substitute.For<IFileWriter>();
        var controller = new FileTransferController();
        var file = FormFile(1, fileName: "   ");

        var response = await controller.UploadCaseFile(writer, Guid.CreateVersion7(), file, CancellationToken.None);

        AssertProblem(response.Result, 400);
        Assert.That(writer.ReceivedCalls(), Is.Empty, "an empty file name must never reach the writer");
    }

    [Test]
    public async Task AnUploadWithAMediaTypeOverTheColumnLimitIsRefusedWithFourHundred()
    {
        var writer = Substitute.For<IFileWriter>();
        var controller = new FileTransferController();
        var file = FormFile(1, contentType: new string('a', FileLimits.MaxMediaTypeLength + 1));

        var response = await controller.UploadCaseFile(writer, Guid.CreateVersion7(), file, CancellationToken.None);

        AssertProblem(response.Result, 400);
        Assert.That(writer.ReceivedCalls(), Is.Empty, "a media type over the FileAsset.MediaType column limit must never reach the writer");
    }

    [Test]
    public async Task AnUploadWithNoMediaTypeIsAcceptedWithANullMediaType()
    {
        FileUpload? upload = null;
        var writer = UploadingWriter(Uploaded(Item()), captured => upload = captured);
        var controller = new FileTransferController();
        var file = FormFile(1, contentType: null);

        await controller.UploadCaseFile(writer, Guid.CreateVersion7(), file, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload, Is.Not.Null, "a multipart part with no content type must reach the writer");
            Assert.That(upload?.MediaType, Is.Null, "a missing content type stores a null media type, not an exception");
        }
    }

    [Test]
    public async Task AnUploadAnswersCreatedAtTheContentOfTheNewFile()
    {
        var created = Item();
        var writer = UploadingWriter(Uploaded(created));
        var controller = new FileTransferController();

        var response = await controller.UploadCaseFile(writer, Guid.CreateVersion7(), FormFile(1), CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)response.Result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(201), "an upload answers 201, not 200");
            Assert.That(result.ActionName, Is.EqualTo(nameof(FileTransferController.DownloadFileContent)), "the Location names the download action of the new file");
            Assert.That(result.RouteValues?["fileId"], Is.EqualTo(created.FileId));
            Assert.That(result.Value, Is.SameAs(created));
        }
    }

    [Test]
    public async Task AnUploadOntoAMissingCaseIsAProblemWithFourOhFour()
    {
        var writer = UploadingWriter(new UploadFileResult { Outcome = UploadFileOutcome.OwnerNotFound });
        var controller = new FileTransferController();

        var response = await controller.UploadCaseFile(writer, Guid.CreateVersion7(), FormFile(1), CancellationToken.None);

        AssertProblem(response.Result, 404);
    }

    [Test]
    public void AnUploadOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = UploadingWriter(new UploadFileResult { Outcome = (UploadFileOutcome)99 });
        var controller = new FileTransferController();

        Assert.ThrowsAsync<UnreachableException>(
            async () => await controller.UploadCaseFile(writer, Guid.CreateVersion7(), FormFile(1), CancellationToken.None),
            "an outcome the endpoint does not name never turns into a status");
    }

    [Test]
    public void TheUploadDeclaresItsOwnRequestSizeLimit()
    {
        var method = typeof(FileTransferController).GetMethod(nameof(FileTransferController.UploadCaseFile))!;
        var attribute = method.GetCustomAttributes(typeof(RequestSizeLimitAttribute), inherit: false).Single();

        Assert.That(
            ((IRequestSizeLimitMetadata)attribute).MaxRequestBodySize,
            Is.EqualTo(FileLimits.MaxUploadRequestBytes),
            "Kestrel's 30 MB default would cap the upload below the product limit");
    }

    [Test]
    public async Task ADownloadIsAnAttachmentUnderTheStoredName()
    {
        var reader = DownloadingReader(new FileDownload { FileName = "smlouva.pdf", MediaType = "application/pdf", Content = new MemoryStream("abc"u8.ToArray()) });

        var controller = new FileTransferController();

        var result = await controller.DownloadFileContent(reader, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<FileStreamResult>());
        var fileResult = (FileStreamResult)result;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fileResult.FileDownloadName, Is.EqualTo("smlouva.pdf"));
            Assert.That(fileResult.ContentType, Is.EqualTo("application/pdf"));
        }
    }

    [Test]
    public async Task ADownloadOfAMissingFileIsAProblemWithFourOhFour()
    {
        var reader = DownloadingReader(download: null);
        var controller = new FileTransferController();

        var result = await controller.DownloadFileContent(reader, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task AnActUploadOverTheLimitIsRefusedWithFourThirteen()
    {
        var writer = Substitute.For<IFileWriter>();
        var controller = new FileTransferController();
        var file = FormFile(FileLimits.MaxUploadBytes + 1);

        var response = await controller.UploadActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), file, CancellationToken.None);

        AssertProblem(response.Result, 413);
        Assert.That(writer.ReceivedCalls(), Is.Empty, "an upload over the limit must never reach the writer");
    }

    [Test]
    public async Task AnActUploadReachesTheWriterWithBothRouteIds()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var writer = UploadingWriter(Uploaded(Item()));
        var controller = new FileTransferController();

        await controller.UploadActFile(writer, caseId, actId, FormFile(1), CancellationToken.None);

        await writer
            .Received(1)
            .UploadActFile(caseId, actId, Arg.Any<FileUpload>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AnActUploadWithAFileNameOverTheColumnLimitIsRefusedWithFourHundred()
    {
        var writer = Substitute.For<IFileWriter>();
        var controller = new FileTransferController();
        var file = FormFile(1, fileName: new string('a', FileLimits.MaxFileNameLength + 1) + ".pdf");

        var response = await controller.UploadActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), file, CancellationToken.None);

        AssertProblem(response.Result, 400);
        Assert.That(writer.ReceivedCalls(), Is.Empty, "a file name over the FileAsset.FileName column limit must never reach the writer");
    }

    [Test]
    public async Task AnActUploadWithAnEmptyFileNameIsRefusedWithFourHundred()
    {
        var writer = Substitute.For<IFileWriter>();
        var controller = new FileTransferController();
        var file = FormFile(1, fileName: "   ");

        var response = await controller.UploadActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), file, CancellationToken.None);

        AssertProblem(response.Result, 400);
        Assert.That(writer.ReceivedCalls(), Is.Empty, "an empty file name must never reach the writer");
    }

    [Test]
    public async Task AnActUploadWithAMediaTypeOverTheColumnLimitIsRefusedWithFourHundred()
    {
        var writer = Substitute.For<IFileWriter>();
        var controller = new FileTransferController();
        var file = FormFile(1, contentType: new string('a', FileLimits.MaxMediaTypeLength + 1));

        var response = await controller.UploadActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), file, CancellationToken.None);

        AssertProblem(response.Result, 400);
        Assert.That(writer.ReceivedCalls(), Is.Empty, "a media type over the FileAsset.MediaType column limit must never reach the writer");
    }

    [Test]
    public async Task AnActUploadWithNoMediaTypeIsAcceptedWithANullMediaType()
    {
        FileUpload? upload = null;
        var writer = UploadingWriter(Uploaded(Item()), captured => upload = captured);
        var controller = new FileTransferController();
        var file = FormFile(1, contentType: null);

        await controller.UploadActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), file, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload, Is.Not.Null, "a multipart part with no content type must reach the writer");
            Assert.That(upload?.MediaType, Is.Null, "a missing content type stores a null media type, not an exception");
        }
    }

    [Test]
    public async Task AnActUploadAnswersCreatedAtTheContentOfTheNewFile()
    {
        var created = Item();
        var writer = UploadingWriter(Uploaded(created));
        var controller = new FileTransferController();

        var response = await controller.UploadActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), FormFile(1), CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)response.Result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(201));
            Assert.That(result.ActionName, Is.EqualTo(nameof(FileTransferController.DownloadFileContent)));
            Assert.That(result.RouteValues?["fileId"], Is.EqualTo(created.FileId));
            Assert.That(result.Value, Is.SameAs(created));
        }
    }

    [Test]
    public async Task AnUploadOntoAMissingActIsAProblemWithFourOhFour()
    {
        var writer = UploadingWriter(new UploadFileResult { Outcome = UploadFileOutcome.OwnerNotFound });
        var controller = new FileTransferController();

        var response = await controller.UploadActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), FormFile(1), CancellationToken.None);

        var problem = AssertProblem(response.Result, 404);
        Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNotFound), "the act's 404 names the act, not the case");
    }

    [Test]
    public void AnActUploadOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = UploadingWriter(new UploadFileResult { Outcome = (UploadFileOutcome)99 });
        var controller = new FileTransferController();

        Assert.ThrowsAsync<UnreachableException>(
            async () => await controller.UploadActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), FormFile(1), CancellationToken.None),
            "an outcome the endpoint does not name never turns into a status");
    }

    [Test]
    public void TheActUploadDeclaresItsOwnRequestSizeLimit()
    {
        var method = typeof(FileTransferController).GetMethod(nameof(FileTransferController.UploadActFile))!;
        var attribute = method.GetCustomAttributes(typeof(RequestSizeLimitAttribute), inherit: false).Single();

        Assert.That(
            ((IRequestSizeLimitMetadata)attribute).MaxRequestBodySize,
            Is.EqualTo(FileLimits.MaxUploadRequestBytes),
            "Kestrel's 30 MB default would cap the upload below the product limit");
    }

    private static IFileWriter UploadingWriter(UploadFileResult result, Action<FileUpload>? capture = null)
    {
        var writer = Substitute.For<IFileWriter>();
        writer
            .UploadCaseFile(Arg.Any<Guid>(), Arg.Any<FileUpload>(), Arg.Any<CancellationToken>())
            .Returns(result)
            .AndDoes(call => capture?.Invoke(call.Arg<FileUpload>()));
        writer
            .UploadActFile(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<FileUpload>(), Arg.Any<CancellationToken>())
            .Returns(result)
            .AndDoes(call => capture?.Invoke(call.Arg<FileUpload>()));

        return writer;
    }

    private static IFileReader DownloadingReader(FileDownload? download)
    {
        var reader = Substitute.For<IFileReader>();
        reader
            .OpenFileContent(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(download);

        return reader;
    }

    private static FileListItem Item()
    {
        return new() { FileId = Guid.CreateVersion7(), FileName = "a.pdf", SizeBytes = 1, Created = DateTime.UtcNow };
    }

    private static UploadFileResult Uploaded(FileListItem file)
    {
        return new UploadFileResult { Outcome = UploadFileOutcome.Uploaded, File = file };
    }

    // The controller never reads the backing stream when a substitute stands in for the writer, so the
    // stream itself stays empty; only the reported Length matters.
    private static FormFile FormFile(long length, string fileName = "a.pdf", string? contentType = "application/pdf")
    {
        return new FormFile(new MemoryStream(), 0, length, "file", fileName) { Headers = new HeaderDictionary(), ContentType = contentType! };
    }
}
