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
        reader.ListFileSigners(fileId, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(signers);
        var controller = new FileSignersController();

        var response = await controller.ListFileSigners(reader, fileId, version: null, CancellationToken.None);

        Assert.That(((OkObjectResult)response.Result!).Value, Is.EqualTo(signers));
    }

    [Test]
    public async Task TheVersionInTheQueryReachesTheReaderAndTheVerifier()
    {
        var fileId = Guid.CreateVersion7();
        var reader = Substitute.For<IFileSignerReader>();
        var verifier = Substitute.For<IFileSignerVerifier>();
        var controller = new FileSignersController();

        await controller.ListFileSigners(reader, fileId, version: 2, CancellationToken.None);
        await controller.VerifyFileSigners(verifier, fileId, version: 3, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            await reader
                .Received(1)
                .ListFileSigners(fileId, 2, Arg.Any<CancellationToken>());
            await verifier
                .Received(1)
                .VerifyFileSigners(fileId, 3, Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task AnUnknownFileIsAProblemWithFourOhFour()
    {
        var reader = Substitute.For<IFileSignerReader>();
        reader.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns((FileSignersResponse?)null);
        var controller = new FileSignersController();

        var response = await controller.ListFileSigners(reader, Guid.CreateVersion7(), version: null, CancellationToken.None);

        var problem = AssertProblem(response.Result, 404);
        Assert.That(problem.Title, Is.EqualTo(FileProblems.NotFound));
    }

    [Test]
    public async Task TheVerifiedSignersOfTheFileInTheRouteComeBack()
    {
        var fileId = Guid.CreateVersion7();
        var signer = new FileSigner { Name = "Jan Testovací", Kind = FileSignerKind.QualifiedSignature, State = FileSignatureState.Valid };
        var signers = new FileSignersResponse { Signers = [signer] };
        var verifier = Substitute.For<IFileSignerVerifier>();
        verifier.VerifyFileSigners(fileId, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(signers);
        var controller = new FileSignersController();

        var response = await controller.VerifyFileSigners(verifier, fileId, version: null, CancellationToken.None);

        Assert.That(((OkObjectResult)response.Result!).Value, Is.EqualTo(signers));
    }

    [Test]
    public async Task VerifyingAnUnknownFileIsAProblemWithFourOhFour()
    {
        var verifier = Substitute.For<IFileSignerVerifier>();
        verifier.VerifyFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns((FileSignersResponse?)null);
        var controller = new FileSignersController();

        var response = await controller.VerifyFileSigners(verifier, Guid.CreateVersion7(), version: null, CancellationToken.None);

        var problem = AssertProblem(response.Result, 404);
        Assert.That(problem.Title, Is.EqualTo(FileProblems.NotFound));
    }
}
