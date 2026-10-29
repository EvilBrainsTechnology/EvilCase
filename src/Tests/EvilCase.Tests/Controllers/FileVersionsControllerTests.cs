using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class FileVersionsControllerTests
{
    [Test]
    public async Task TheListIsAskedForTheFileInTheRoute()
    {
        var fileId = Guid.CreateVersion7();
        var items = new List<FileVersionItem> { new() { Number = 2, SizeBytes = 5, Created = DateTime.UtcNow, UploaderEmail = "a@example.com" } };
        var reader = Substitute.For<IFileReader>();
        reader.ListFileVersions(fileId, Arg.Any<CancellationToken>()).Returns(items);
        var controller = new FileVersionsController();

        var response = await controller.ListFileVersions(reader, fileId, CancellationToken.None);

        var ok = (OkObjectResult)response.Result!;
        Assert.That(((FileVersionListResponse)ok.Value!).Items, Is.EqualTo(items));
    }

    [Test]
    public async Task ListingTheVersionsOfAMissingFileIsAProblemWithFourOhFour()
    {
        var reader = Substitute.For<IFileReader>();
        reader.ListFileVersions(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<FileVersionItem>?)null);
        var controller = new FileVersionsController();

        var response = await controller.ListFileVersions(reader, Guid.CreateVersion7(), CancellationToken.None);

        var problem = AssertProblem(response.Result, 404);
        Assert.That(problem.Title, Is.EqualTo(FileProblems.NotFound));
    }

    [Test]
    public async Task TheDeleteReachesTheWriterWithTheFileAndTheNumber()
    {
        var fileId = Guid.CreateVersion7();
        var writer = DeletingWriter(FileVersionDeleteOutcome.Deleted);
        var controller = new FileVersionsController();

        await controller.DeleteFileVersion(writer, fileId, 2, CancellationToken.None);

        await writer
            .Received(1)
            .DeleteFileVersion(fileId, 2, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeletingAVersionAnswersWithNoContent()
    {
        var controller = new FileVersionsController();

        var result = await controller.DeleteFileVersion(DeletingWriter(FileVersionDeleteOutcome.Deleted), Guid.CreateVersion7(), 2, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAMissingVersionIsAProblemWithFourOhFour()
    {
        var controller = new FileVersionsController();

        var result = await controller.DeleteFileVersion(DeletingWriter(FileVersionDeleteOutcome.NotFound), Guid.CreateVersion7(), 2, CancellationToken.None);

        var problem = AssertProblem(result, 404);
        Assert.That(problem.Title, Is.EqualTo(FileProblems.VersionNotFound));
    }

    [Test]
    public async Task DeletingTheOnlyVersionIsAProblemWithFourOhNine()
    {
        var controller = new FileVersionsController();

        var result = await controller.DeleteFileVersion(DeletingWriter(FileVersionDeleteOutcome.LastVersion), Guid.CreateVersion7(), 1, CancellationToken.None);

        var problem = AssertProblem(result, 409);
        Assert.That(problem.Title, Is.EqualTo(FileProblems.LastVersion));
    }

    [Test]
    public async Task ADeleteOutcomeTheEndpointDoesNotKnowThrows()
    {
        var controller = new FileVersionsController();

        await Assert.ThatAsync(
            async () => await controller.DeleteFileVersion(DeletingWriter((FileVersionDeleteOutcome)99), Guid.CreateVersion7(), 1, CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    private static IFileWriter DeletingWriter(FileVersionDeleteOutcome outcome)
    {
        var writer = Substitute.For<IFileWriter>();
        writer
            .DeleteFileVersion(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }
}
