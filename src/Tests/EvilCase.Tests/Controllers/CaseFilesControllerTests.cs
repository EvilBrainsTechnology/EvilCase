using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CaseFilesControllerTests
{
    [Test]
    public async Task TheListIsAskedForTheCaseInTheRoute()
    {
        var caseId = Guid.CreateVersion7();
        var reader = ListingReader([]);
        var controller = new CaseFilesController();

        await controller.ListCaseFiles(reader, caseId, CancellationToken.None);

        await reader.Received(1).ListCaseFiles(caseId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheListedItemsComeBackInTheOrderTheReaderGaveThem()
    {
        var reader = ListingReader([Item("prvni.txt"), Item("druhy.txt")]);
        var controller = new CaseFilesController();

        var response = await controller.ListCaseFiles(reader, Guid.CreateVersion7(), CancellationToken.None);

        var body = (FileListResponse)((OkObjectResult)response.Result!).Value!;

        Assert.That(body.Items.Select(static item => item.FileName), Is.EqualTo(["prvni.txt", "druhy.txt"]));
    }

    [Test]
    public async Task AListOnAMissingCaseIsAProblemWithFourOhFour()
    {
        var reader = ListingReader(items: null);
        var controller = new CaseFilesController();

        var response = await controller.ListCaseFiles(reader, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(response.Result, 404);
    }

    [Test]
    public async Task TheDeleteReachesTheWriterWithBothRouteIds()
    {
        var caseId = Guid.CreateVersion7();
        var fileId = Guid.CreateVersion7();
        var writer = DeletingWriter(DeleteOutcome.Deleted);
        var controller = new CaseFilesController();

        await controller.DeleteCaseFile(writer, caseId, fileId, CancellationToken.None);

        await writer.Received(1).DeleteCaseFile(caseId, fileId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeletingAFileAnswersWithNoContent()
    {
        var writer = DeletingWriter(DeleteOutcome.Deleted);
        var controller = new CaseFilesController();

        var result = await controller.DeleteCaseFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAMissingFileIsAProblemWithFourOhFour()
    {
        var writer = DeletingWriter(DeleteOutcome.NotFound);
        var controller = new CaseFilesController();

        var result = await controller.DeleteCaseFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task ADeleteOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = DeletingWriter((DeleteOutcome)99);
        var controller = new CaseFilesController();

        await Assert.ThatAsync(
            async () => await controller.DeleteCaseFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    private static IFileReader ListingReader(IReadOnlyList<FileListItem>? items)
    {
        var reader = Substitute.For<IFileReader>();
        reader.ListCaseFiles(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(items);

        return reader;
    }

    private static IFileWriter DeletingWriter(DeleteOutcome outcome)
    {
        var writer = Substitute.For<IFileWriter>();
        writer.DeleteCaseFile(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static FileListItem Item(string fileName)
    {
        return new() { FileId = Guid.CreateVersion7(), FileName = fileName, SizeBytes = 1, Created = DateTime.UtcNow };
    }
}
