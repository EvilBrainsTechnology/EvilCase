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
        var writer = UploadingWriter();
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
        var writer = UploadingWriter(captured => upload = captured);
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
        var writer = UploadingWriter(captured => upload = captured);
        var controller = new FileTransferController();
        var file = FormFile(1, contentType: null);

        await controller.UploadCaseFile(writer, Guid.CreateVersion7(), file, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload, Is.Not.Null, "a multipart part with no content type must reach the writer");
            Assert.That(upload?.MediaType, Is.Null, "a missing content type stores a null media type, not an exception");
        }
    }

    [TestCase(nameof(FileTransferController.UploadCaseFile))]
    [TestCase(nameof(FileTransferController.UploadActFile))]
    [TestCase(nameof(FileTransferController.UploadFileVersion))]
    public void AnUploadDeclaresItsOwnRequestSizeLimit(string action)
    {
        var method = typeof(FileTransferController).GetMethod(action)!;
        var attribute = method.GetCustomAttributes(typeof(RequestSizeLimitAttribute), inherit: false).Single();

        Assert.That(
            ((IRequestSizeLimitMetadata)attribute).MaxRequestBodySize,
            Is.EqualTo(FileLimits.MaxUploadRequestBytes),
            "Kestrel's 30 MB default would cap the upload below the product limit");
    }

    private static IFileWriter UploadingWriter(Action<FileUpload>? capture = null)
    {
        var file = new FileListItem { FileId = Guid.CreateVersion7(), FileName = "a.pdf", SizeBytes = 1, Created = DateTime.UtcNow, Version = 1, VersionCount = 1 };
        var writer = Substitute.For<IFileWriter>();
        writer
            .UploadCaseFile(Arg.Any<Guid>(), Arg.Any<FileUpload>(), Arg.Any<CancellationToken>())
            .Returns(new UploadFileResult { Outcome = UploadFileOutcome.Uploaded, File = file })
            .AndDoes(call => capture?.Invoke(call.Arg<FileUpload>()));

        return writer;
    }

    // The controller never reads the backing stream when a substitute stands in for the writer, so the
    // stream itself stays empty; only the reported Length matters.
    private static FormFile FormFile(long length, string fileName = "a.pdf", string? contentType = "application/pdf")
    {
        return new FormFile(new MemoryStream(), 0, length, "file", fileName) { Headers = new HeaderDictionary(), ContentType = contentType! };
    }
}
