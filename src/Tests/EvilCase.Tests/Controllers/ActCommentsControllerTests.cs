using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Comments;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ActCommentsControllerTests
{
    [Test]
    public async Task EditingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = Substitute.For<ICommentWriter>();
        writer
            .UpdateActComment(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CommentEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(CommentWriteOutcome.NotAuthor);
        var controller = new ActCommentsController();

        var result = await controller.EditActComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "x" }, CancellationToken.None);

        AssertProblem(result, 403);
    }

    [Test]
    public async Task DeletingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = Substitute.For<ICommentWriter>();
        writer
            .DeleteActComment(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CommentWriteOutcome.NotAuthor);
        var controller = new ActCommentsController();

        var result = await controller.DeleteActComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 403);
    }
}
