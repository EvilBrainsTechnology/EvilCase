using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Files;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CaseFilesControllerTests
{
    [Test]
    public async Task RenamingKeepsOnlyTheLastPathSegment()
    {
        var writer = RenamingWriter();
        var controller = new CaseFilesController();

        await controller.RenameCaseFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new FileRenameRequest { FileName = "slozka/podslozka/novy.pdf" }, CancellationToken.None);

        await writer
            .Received(1)
            .RenameCaseFile(Arg.Any<Guid>(), Arg.Any<Guid>(), "novy.pdf", Arg.Any<CancellationToken>());
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("slozka/")]
    public async Task RenamingToABlankNameIsAProblemWithFourHundredAndTheWriterIsNotReached(string fileName)
    {
        var writer = RenamingWriter();
        var controller = new CaseFilesController();

        var result = await controller.RenameCaseFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new FileRenameRequest { FileName = fileName }, CancellationToken.None);

        AssertProblem(result, 400);
        await writer
            .DidNotReceive()
            .RenameCaseFile(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static IFileWriter RenamingWriter()
    {
        var writer = Substitute.For<IFileWriter>();
        writer
            .RenameCaseFile(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(FileRenameOutcome.Renamed);

        return writer;
    }
}
