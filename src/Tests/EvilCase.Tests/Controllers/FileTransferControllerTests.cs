using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class FileTransferControllerTests
{
    [Test]
    public async Task AnUploadOverTheLimitIsRefusedWithFourThirteen()
    {
        var writer = new RecordingFileWriter { UploadResult = new UploadFileResult { Outcome = UploadFileOutcome.CaseNotFound } };
        var controller = new FileTransferController();
        var file = FormFile(FileLimits.MaxUploadBytes + 1);

        var response = await controller.UploadCaseFile(writer, Guid.CreateVersion7(), file, CancellationToken.None);

        AssertProblem(response.Result, 413);
        Assert.That(writer.Called, Is.False, "an upload over the limit must never reach the writer");
    }

    [Test]
    public async Task AnUploadAtTheLimitReachesTheWriter()
    {
        var writer = new RecordingFileWriter { UploadResult = Uploaded(Item()) };
        var controller = new FileTransferController();
        var file = FormFile(FileLimits.MaxUploadBytes);

        await controller.UploadCaseFile(writer, Guid.CreateVersion7(), file, CancellationToken.None);

        Assert.That(writer.Called, Is.True, "an upload at the limit must reach the writer");
    }

    [Test]
    public async Task AnUploadKeepsOnlyTheNameItArrivedUnder()
    {
        var writer = new RecordingFileWriter { UploadResult = Uploaded(Item()) };
        var controller = new FileTransferController();
        var file = FormFile(1, fileName: "../../evil.txt");

        await controller.UploadCaseFile(writer, Guid.CreateVersion7(), file, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.Upload!.FileName, Is.EqualTo("evil.txt"), "an upload keeps only the name it arrived under, not the path");
            Assert.That(writer.Upload.MediaType, Is.EqualTo("application/pdf"));
        }
    }

    [Test]
    public async Task AnUploadAnswersCreatedAtTheContentOfTheNewFile()
    {
        var created = Item();
        var writer = new RecordingFileWriter { UploadResult = Uploaded(created) };
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
        var writer = new RecordingFileWriter { UploadResult = new UploadFileResult { Outcome = UploadFileOutcome.CaseNotFound } };
        var controller = new FileTransferController();

        var response = await controller.UploadCaseFile(writer, Guid.CreateVersion7(), FormFile(1), CancellationToken.None);

        AssertProblem(response.Result, 404);
    }

    [Test]
    public void AnUploadOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = new RecordingFileWriter { UploadResult = new UploadFileResult { Outcome = (UploadFileOutcome)99 } };
        var controller = new FileTransferController();

        Assert.ThrowsAsync<UnreachableException>(
            () => controller.UploadCaseFile(writer, Guid.CreateVersion7(), FormFile(1), CancellationToken.None),
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
        var reader = new RecordingFileReader { Download = new FileDownload { FileName = "smlouva.pdf", MediaType = "application/pdf", Content = new MemoryStream("abc"u8.ToArray()) } };

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
        var reader = new RecordingFileReader { Download = null };
        var controller = new FileTransferController();

        var result = await controller.DownloadFileContent(reader, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    private static ProblemDetails AssertProblem(IActionResult? result, in int statusCode)
    {
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(objectResult.StatusCode, Is.EqualTo(statusCode));
            Assert.That(objectResult.Value, Is.InstanceOf<ProblemDetails>());
        }

        return (ProblemDetails)objectResult.Value!;
    }

    private static FileListItem Item()
    {
        return new() { FileId = Guid.CreateVersion7(), FileName = "a.pdf", SizeBytes = 1, Created = DateTime.UtcNow };
    }

    private static UploadFileResult Uploaded(FileListItem file)
    {
        return new UploadFileResult { Outcome = UploadFileOutcome.Uploaded, File = file };
    }

    // The controller never reads the backing stream when the recording writer stands in, so the stream
    // itself stays empty; only the reported Length matters.
    private static FormFile FormFile(long length, string fileName = "a.pdf")
    {
        return new FormFile(new MemoryStream(), 0, length, "file", fileName) { Headers = new HeaderDictionary(), ContentType = "application/pdf" };
    }

    private sealed class RecordingFileReader : IFileReader
    {
        public FileDownload? Download { get; init; }

        public Task<IReadOnlyList<FileListItem>?> ListCaseFiles(Guid caseId, CancellationToken token)
        {
            return Task.FromResult<IReadOnlyList<FileListItem>?>(null);
        }

        public Task<FileDownload?> OpenFileContent(Guid fileId, CancellationToken token)
        {
            return Task.FromResult(this.Download);
        }
    }

    private sealed class RecordingFileWriter : IFileWriter
    {
        public bool Called { get; private set; }

        public FileUpload? Upload { get; private set; }

        public required UploadFileResult UploadResult { get; init; }

        public Task<UploadFileResult> UploadCaseFile(Guid caseId, FileUpload upload, CancellationToken token)
        {
            this.Called = true;
            this.Upload = upload;

            return Task.FromResult(this.UploadResult);
        }

        public Task<FileDeleteOutcome> DeleteCaseFile(Guid caseId, Guid fileId, CancellationToken token)
        {
            return Task.FromResult(FileDeleteOutcome.NotFound);
        }
    }
}
