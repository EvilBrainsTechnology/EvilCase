using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ActFilesControllerTests
{
    [Test]
    public async Task TheListIsAskedForTheActInTheRoute()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var reader = new RecordingFileReader { ListResult = [] };
        var controller = new ActFilesController();

        await controller.ListActFiles(reader, caseId, actId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reader.ListCaseId, Is.EqualTo(caseId));
            Assert.That(reader.ListActId, Is.EqualTo(actId));
        }
    }

    [Test]
    public async Task TheListedItemsComeBackInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingFileReader { ListResult = [Item("prvni.txt"), Item("druhy.txt")] };
        var controller = new ActFilesController();

        var response = await controller.ListActFiles(reader, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        var body = (FileListResponse)((OkObjectResult)response.Result!).Value!;

        Assert.That(body.Items.Select(item => item.FileName), Is.EqualTo(["prvni.txt", "druhy.txt"]));
    }

    [Test]
    public async Task AListOnAMissingActIsAProblemWithFourOhFour()
    {
        var reader = new RecordingFileReader { ListResult = null };
        var controller = new ActFilesController();

        var response = await controller.ListActFiles(reader, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        var problem = AssertProblem(response.Result, 404);
        Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNotFound));
    }

    [Test]
    public async Task TheDeleteReachesTheWriterWithEveryRouteId()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var fileId = Guid.CreateVersion7();
        var writer = new RecordingFileWriter { DeleteOutcome = FileDeleteOutcome.Deleted };
        var controller = new ActFilesController();

        await controller.DeleteActFile(writer, caseId, actId, fileId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.DeleteCaseId, Is.EqualTo(caseId));
            Assert.That(writer.DeleteActId, Is.EqualTo(actId));
            Assert.That(writer.DeleteFileId, Is.EqualTo(fileId));
        }
    }

    [Test]
    public async Task DeletingAFileAnswersWithNoContent()
    {
        var writer = new RecordingFileWriter { DeleteOutcome = FileDeleteOutcome.Deleted };
        var controller = new ActFilesController();

        var result = await controller.DeleteActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAMissingFileIsAProblemWithFourOhFour()
    {
        var writer = new RecordingFileWriter { DeleteOutcome = FileDeleteOutcome.NotFound };
        var controller = new ActFilesController();

        var result = await controller.DeleteActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task ADeleteOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = new RecordingFileWriter { DeleteOutcome = (FileDeleteOutcome)99 };
        var controller = new ActFilesController();

        await Assert.ThatAsync(
            () => controller.DeleteActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
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

    private static FileListItem Item(string fileName)
    {
        return new() { FileId = Guid.CreateVersion7(), FileName = fileName, SizeBytes = 1, Created = DateTime.UtcNow };
    }
}
