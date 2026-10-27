using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Files.Signatures;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class FileSignersControllerTests
{
    [Test]
    public async Task TheSignersOfTheFileInTheRouteComeBack()
    {
        var fileId = Guid.CreateVersion7();
        var signers = new FileSignersResponse { Signers = [new FileSigner { Name = "Jan Testovací", Kind = FileSignerKind.QualifiedSignature }] };
        var reader = Substitute.For<IFileSignerReader>();
        reader.ListFileSigners(fileId, Arg.Any<CancellationToken>()).Returns(signers);
        var controller = new FileSignersController();

        var response = await controller.ListFileSigners(reader, fileId, CancellationToken.None);

        Assert.That(((OkObjectResult)response.Result!).Value, Is.EqualTo(signers));
    }

    [Test]
    public async Task AnUnknownFileIsAProblemWithFourOhFour()
    {
        var reader = Substitute.For<IFileSignerReader>();
        reader.ListFileSigners(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((FileSignersResponse?)null);
        var controller = new FileSignersController();

        var response = await controller.ListFileSigners(reader, Guid.CreateVersion7(), CancellationToken.None);

        var problem = AssertProblem(response.Result, 404);
        Assert.That(problem.Title, Is.EqualTo(FileProblems.NotFound));
    }
}
