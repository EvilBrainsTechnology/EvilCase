using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Files;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class FilesControllerTests
{
    [Test]
    public async Task RenamingKeepsOnlyTheLastPathSegment()
    {
        var writer = RenamingWriter();
        var controller = new FilesController();

        await controller.RenameFile(writer, Guid.CreateVersion7(), new FileRenameRequest { FileName = "slozka/podslozka/novy.pdf" }, CancellationToken.None);

        await writer
            .Received(1)
            .RenameFile(Arg.Any<Guid>(), "novy.pdf", Arg.Any<CancellationToken>());
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("slozka/")]
    public async Task RenamingToABlankNameIsAProblemWithFourHundredAndTheWriterIsNotReached(string fileName)
    {
        var writer = RenamingWriter();
        var controller = new FilesController();

        var result = await controller.RenameFile(writer, Guid.CreateVersion7(), new FileRenameRequest { FileName = fileName }, CancellationToken.None);

        AssertProblem(result, 400);
        await writer
            .DidNotReceive()
            .RenameFile(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static IFileWriter RenamingWriter()
    {
        var writer = Substitute.For<IFileWriter>();
        writer
            .RenameFile(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(FileRenameOutcome.Renamed);

        return writer;
    }
}
