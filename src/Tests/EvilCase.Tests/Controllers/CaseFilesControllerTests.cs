using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Entities;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CaseFilesControllerTests
{
    [Test]
    public async Task TheListIsAskedForTheCaseInTheRoute()
    {
        var caseId = Guid.CreateVersion7();
        var reader = new RecordingFileReader { ListResult = [] };
        var controller = new CaseFilesController();

        await controller.ListCaseFiles(reader, caseId, CancellationToken.None);

        Assert.That(reader.ListCaseId, Is.EqualTo(caseId));
    }

    [Test]
    public async Task TheListedItemsComeBackInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingFileReader { ListResult = [Item("prvni.txt"), Item("druhy.txt")] };
        var controller = new CaseFilesController();

        var response = await controller.ListCaseFiles(reader, Guid.CreateVersion7(), CancellationToken.None);

        var body = (FileListResponse)((OkObjectResult)response.Result!).Value!;

        Assert.That(body.Items.Select(item => item.FileName), Is.EqualTo(["prvni.txt", "druhy.txt"]));
    }

    [Test]
    public async Task AListOnAMissingCaseIsAProblemWithFourOhFour()
    {
        var reader = new RecordingFileReader { ListResult = null };
        var controller = new CaseFilesController();

        var response = await controller.ListCaseFiles(reader, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(response.Result, 404);
    }

    [Test]
    public async Task TheDeleteReachesTheWriterWithBothRouteIds()
    {
        var caseId = Guid.CreateVersion7();
        var fileId = Guid.CreateVersion7();
        var writer = new RecordingFileWriter { DeleteOutcome = DeleteOutcome.Deleted };
        var controller = new CaseFilesController();

        await controller.DeleteCaseFile(writer, caseId, fileId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.DeleteCaseId, Is.EqualTo(caseId));
            Assert.That(writer.DeleteFileId, Is.EqualTo(fileId));
        }
    }

    [Test]
    public async Task DeletingAFileAnswersWithNoContent()
    {
        var writer = new RecordingFileWriter { DeleteOutcome = DeleteOutcome.Deleted };
        var controller = new CaseFilesController();

        var result = await controller.DeleteCaseFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAMissingFileIsAProblemWithFourOhFour()
    {
        var writer = new RecordingFileWriter { DeleteOutcome = DeleteOutcome.NotFound };
        var controller = new CaseFilesController();

        var result = await controller.DeleteCaseFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task ADeleteOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = new RecordingFileWriter { DeleteOutcome = (DeleteOutcome)99 };
        var controller = new CaseFilesController();

        await Assert.ThatAsync(
            () => controller.DeleteCaseFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    private static FileListItem Item(string fileName)
    {
        return new() { FileId = Guid.CreateVersion7(), FileName = fileName, SizeBytes = 1, Created = DateTime.UtcNow };
    }
}
