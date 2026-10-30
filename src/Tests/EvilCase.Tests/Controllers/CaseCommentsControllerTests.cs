using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Comments;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CaseCommentsControllerTests
{
    [Test]
    public async Task EditingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = Substitute.For<ICommentWriter>();
        writer
            .UpdateCaseComment(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CommentEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(CommentWriteOutcome.NotAuthor);
        var controller = new CaseCommentsController();

        var result = await controller.EditCaseComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "x" }, CancellationToken.None);

        AssertProblem(result, 403);
    }

    [Test]
    public async Task DeletingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = Substitute.For<ICommentWriter>();
        writer
            .DeleteCaseComment(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CommentWriteOutcome.NotAuthor);
        var controller = new CaseCommentsController();

        var result = await controller.DeleteCaseComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 403);
    }
}
