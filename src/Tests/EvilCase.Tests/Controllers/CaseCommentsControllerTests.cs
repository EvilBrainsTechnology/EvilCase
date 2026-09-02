using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Comments;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CaseCommentsControllerTests
{
    [Test]
    public async Task TheListIsAskedForTheCaseInTheRoute()
    {
        var caseId = Guid.CreateVersion7();
        var reader = Substitute.For<ICommentReader>();
        var controller = new CaseCommentsController();

        await controller.ListCaseComments(reader, caseId, CancellationToken.None);

        await reader
            .Received(1)
            .ListCaseComments(caseId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = Substitute.For<ICommentReader>();
        reader
            .ListCaseComments(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([Item("První"), Item("Druhá")]);
        var controller = new CaseCommentsController();

        var response = await controller.ListCaseComments(reader, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(response.Items.Select(static item => item.Body), Is.EqualTo(["První", "Druhá"]));
    }

    [Test]
    public async Task AddingReachesTheWriterWithTheRouteIdAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var writer = AddingWriter(CommentWriteOutcome.Written);
        var controller = new CaseCommentsController();
        var request = new CommentEditRequest { Body = "Poznámka" };

        var result = await controller.AddCaseComment(writer, caseId, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        await writer
            .Received(1)
            .AddCaseComment(caseId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddingToAMissingCaseIsAProblemWithFourOhFour()
    {
        var writer = AddingWriter(CommentWriteOutcome.NotFound);
        var controller = new CaseCommentsController();

        var result = await controller.AddCaseComment(writer, Guid.CreateVersion7(), new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.EqualTo(CaseProblems.NotFound), "the answer names the case, not the comment");
    }

    [Test]
    public async Task AnAddOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = AddingWriter((CommentWriteOutcome)99);
        var controller = new CaseCommentsController();

        await Assert.ThatAsync(
            async () => await controller.AddCaseComment(writer, Guid.CreateVersion7(), new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    [Test]
    public async Task EditingReachesTheWriterWithBothIdsAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var commentId = Guid.CreateVersion7();
        var writer = EditingWriter(CommentWriteOutcome.Written);
        var controller = new CaseCommentsController();
        var request = new CommentEditRequest { Body = "Upravená" };

        var result = await controller.EditCaseComment(writer, caseId, commentId, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        await writer
            .Received(1)
            .UpdateCaseComment(caseId, commentId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EditingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = EditingWriter(CommentWriteOutcome.NotAuthor);
        var controller = new CaseCommentsController();

        var result = await controller.EditCaseComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "x" }, CancellationToken.None);

        AssertProblem(result, 403);
    }

    [Test]
    public async Task EditingAMissingNoteIsAProblemWithFourOhFour()
    {
        var writer = EditingWriter(CommentWriteOutcome.NotFound);
        var controller = new CaseCommentsController();

        var result = await controller.EditCaseComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "x" }, CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task DeletingReachesTheWriterWithBothIds()
    {
        var caseId = Guid.CreateVersion7();
        var commentId = Guid.CreateVersion7();
        var writer = DeletingWriter(CommentWriteOutcome.Written);
        var controller = new CaseCommentsController();

        var result = await controller.DeleteCaseComment(writer, caseId, commentId, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        await writer
            .Received(1)
            .DeleteCaseComment(caseId, commentId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeletingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = DeletingWriter(CommentWriteOutcome.NotAuthor);
        var controller = new CaseCommentsController();

        var result = await controller.DeleteCaseComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 403);
    }

    [Test]
    public async Task DeletingAMissingNoteIsAProblemWithFourOhFour()
    {
        var writer = DeletingWriter(CommentWriteOutcome.NotFound);
        var controller = new CaseCommentsController();

        var result = await controller.DeleteCaseComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    private static ICommentWriter AddingWriter(CommentWriteOutcome outcome)
    {
        var writer = Substitute.For<ICommentWriter>();
        writer
            .AddCaseComment(Arg.Any<Guid>(), Arg.Any<CommentEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static ICommentWriter EditingWriter(CommentWriteOutcome outcome)
    {
        var writer = Substitute.For<ICommentWriter>();
        writer
            .UpdateCaseComment(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CommentEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static ICommentWriter DeletingWriter(CommentWriteOutcome outcome)
    {
        var writer = Substitute.For<ICommentWriter>();
        writer
            .DeleteCaseComment(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
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
