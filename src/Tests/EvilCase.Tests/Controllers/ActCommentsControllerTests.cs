using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Comments;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ActCommentsControllerTests
{
    [Test]
    public async Task TheListIsAskedForTheCaseAndTheActInTheRoute()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var reader = Substitute.For<ICommentReader>();
        var controller = new ActCommentsController();

        await controller.ListActComments(reader, caseId, actId, CancellationToken.None);

        await reader.Received(1).ListActComments(caseId, actId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = Substitute.For<ICommentReader>();
        reader.ListActComments(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([Item("První"), Item("Druhá")]);
        var controller = new ActCommentsController();

        var response = await controller.ListActComments(reader, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(response.Items.Select(static item => item.Body), Is.EqualTo(["První", "Druhá"]));
    }

    [Test]
    public async Task AddingReachesTheWriterWithBothRouteIdsAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var writer = AddingWriter(CommentWriteOutcome.Written);
        var controller = new ActCommentsController();
        var request = new CommentEditRequest { Body = "Poznámka" };

        var result = await controller.AddActComment(writer, caseId, actId, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        await writer.Received(1).AddActComment(caseId, actId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddingUnderAMissingActIsAProblemWithFourOhFour()
    {
        var writer = AddingWriter(CommentWriteOutcome.NotFound);
        var controller = new ActCommentsController();

        var result = await controller.AddActComment(
            writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNotFound), "the answer names the act, not the comment");
    }

    [Test]
    public async Task AnAddOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = AddingWriter((CommentWriteOutcome)99);
        var controller = new ActCommentsController();

        await Assert.ThatAsync(
            async () => await controller.AddActComment(
                writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    [Test]
    public async Task EditingReachesTheWriterWithBothRouteIdsTheCommentIdAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var commentId = Guid.CreateVersion7();
        var writer = EditingWriter(CommentWriteOutcome.Written);
        var controller = new ActCommentsController();
        var request = new CommentEditRequest { Body = "Upravená" };

        var result = await controller.EditActComment(writer, caseId, actId, commentId, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        await writer.Received(1).UpdateActComment(caseId, actId, commentId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EditingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = EditingWriter(CommentWriteOutcome.NotAuthor);
        var controller = new ActCommentsController();

        var result = await controller.EditActComment(
            writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "x" }, CancellationToken.None);

        AssertProblem(result, 403);
    }

    [Test]
    public async Task EditingAMissingNoteIsAProblemWithFourOhFour()
    {
        var writer = EditingWriter(CommentWriteOutcome.NotFound);
        var controller = new ActCommentsController();

        var result = await controller.EditActComment(
            writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "x" }, CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task AnEditOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = EditingWriter((CommentWriteOutcome)99);
        var controller = new ActCommentsController();

        await Assert.ThatAsync(
            async () => await controller.EditActComment(
                writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "x" }, CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    [Test]
    public async Task DeletingReachesTheWriterWithBothRouteIdsAndTheCommentId()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var commentId = Guid.CreateVersion7();
        var writer = DeletingWriter(CommentWriteOutcome.Written);
        var controller = new ActCommentsController();

        var result = await controller.DeleteActComment(writer, caseId, actId, commentId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        await writer.Received(1).DeleteActComment(caseId, actId, commentId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeletingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = DeletingWriter(CommentWriteOutcome.NotAuthor);
        var controller = new ActCommentsController();

        var result = await controller.DeleteActComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 403);
    }

    [Test]
    public async Task DeletingAMissingNoteIsAProblemWithFourOhFour()
    {
        var writer = DeletingWriter(CommentWriteOutcome.NotFound);
        var controller = new ActCommentsController();

        var result = await controller.DeleteActComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    private static ICommentWriter AddingWriter(CommentWriteOutcome outcome)
    {
        var writer = Substitute.For<ICommentWriter>();
        writer.AddActComment(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CommentEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static ICommentWriter EditingWriter(CommentWriteOutcome outcome)
    {
        var writer = Substitute.For<ICommentWriter>();
        writer.UpdateActComment(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CommentEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static ICommentWriter DeletingWriter(CommentWriteOutcome outcome)
    {
        var writer = Substitute.For<ICommentWriter>();
        writer.DeleteActComment(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static CommentItem Item(string body)
    {
        return new()
        {
            CommentId = Guid.CreateVersion7(),
            Body = body,
            AuthorEmail = "user@example.com",
            IsAuthor = true,
            Created = DateTime.UtcNow,
        };
    }
}
