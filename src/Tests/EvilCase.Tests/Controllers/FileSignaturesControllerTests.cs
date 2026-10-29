using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Files.Signatures;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class FileSignaturesControllerTests
{
    [Test]
    public async Task TheVerdictOfTheFileInTheRouteComesBack()
    {
        var fileId = Guid.CreateVersion7();
        var verdict = new FileSignatureResponse { State = FileSignatureState.Invalid, Problem = FileSignatureProblem.ContentMismatch };
        var reader = Substitute.For<IFileSignatureReader>();
        reader.ReadFileSignature(fileId, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(verdict);
        var controller = new FileSignaturesController();

        var response = await controller.ReadFileSignature(reader, fileId, version: null, CancellationToken.None);

        Assert.That(((OkObjectResult)response.Result!).Value, Is.EqualTo(verdict));
    }

    [Test]
    public async Task TheVersionInTheQueryReachesTheReader()
    {
        var fileId = Guid.CreateVersion7();
        var reader = Substitute.For<IFileSignatureReader>();
        var controller = new FileSignaturesController();

        await controller.ReadFileSignature(reader, fileId, version: 2, CancellationToken.None);

        await reader
            .Received(1)
            .ReadFileSignature(fileId, 2, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AnUnknownFileIsAProblemWithFourOhFour()
    {
        var reader = Substitute.For<IFileSignatureReader>();
        reader.ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns((FileSignatureResponse?)null);
        var controller = new FileSignaturesController();

        var response = await controller.ReadFileSignature(reader, Guid.CreateVersion7(), version: null, CancellationToken.None);

        var problem = AssertProblem(response.Result, 404);
        Assert.That(problem.Title, Is.EqualTo(FileProblems.NotFound));
    }
}
