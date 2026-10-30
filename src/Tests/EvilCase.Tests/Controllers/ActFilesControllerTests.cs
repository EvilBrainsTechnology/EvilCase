using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Files;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ActFilesControllerTests
{
    [Test]
    public async Task RenamingKeepsOnlyTheLastPathSegment()
    {
        var writer = RenamingWriter();
        var controller = new ActFilesController();

        await controller.RenameActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new FileRenameRequest { FileName = "slozka/podslozka/novy.pdf" }, CancellationToken.None);

        await writer
            .Received(1)
            .RenameActFile(Arg.Any<Guid>(), Arg.Any<Guid>(), "novy.pdf", Arg.Any<CancellationToken>());
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("slozka/")]
    public async Task RenamingToABlankNameIsAProblemWithFourHundredAndTheWriterIsNotReached(string fileName)
    {
        var writer = RenamingWriter();
        var controller = new ActFilesController();

        var result = await controller.RenameActFile(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new FileRenameRequest { FileName = fileName }, CancellationToken.None);

        AssertProblem(result, 400);
        await writer
            .DidNotReceive()
            .RenameActFile(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static IFileWriter RenamingWriter()
    {
        var writer = Substitute.For<IFileWriter>();
        writer
            .RenameActFile(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(FileRenameOutcome.Renamed);

        return writer;
    }
}
