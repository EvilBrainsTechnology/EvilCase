using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Comments;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ActCommentsControllerTests
{
    [Test]
    public async Task TheListIsAskedForTheCaseAndTheActInTheRoute()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var reader = new RecordingCommentReader();
        var controller = new ActCommentsController();

        await controller.ListActComments(reader, caseId, actId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reader.CaseId, Is.EqualTo(caseId));
            Assert.That(reader.ActId, Is.EqualTo(actId));
        }
    }

    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingCommentReader { Items = [Item("První"), Item("Druhá")] };
        var controller = new ActCommentsController();

        var response = await controller.ListActComments(reader, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(response.Items.Select(item => item.Body), Is.EqualTo(["První", "Druhá"]));
    }

    [Test]
    public async Task AddingReachesTheWriterWithBothRouteIdsAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var writer = new RecordingCommentWriter { AddOutcome = CommentWriteOutcome.Written };
        var controller = new ActCommentsController();
        var request = new CommentEditRequest { Body = "Poznámka" };

        var result = await controller.AddActComment(writer, caseId, actId, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.AddCaseId, Is.EqualTo(caseId));
            Assert.That(writer.AddActId, Is.EqualTo(actId));
            Assert.That(writer.AddRequest, Is.SameAs(request));
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }
    }

    [Test]
    public async Task AddingUnderAMissingActIsAProblemWithFourOhFour()
    {
        var writer = new RecordingCommentWriter { AddOutcome = CommentWriteOutcome.NotFound };
        var controller = new ActCommentsController();

        var result = await controller.AddActComment(
            writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNotFound), "the answer names the act, not the comment");
    }

    [Test]
    public async Task AnAddOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = new RecordingCommentWriter { AddOutcome = (CommentWriteOutcome)99 };
        var controller = new ActCommentsController();

        await Assert.ThatAsync(
            () => controller.AddActComment(
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
        var writer = new RecordingCommentWriter { UpdateOutcome = CommentWriteOutcome.Written };
        var controller = new ActCommentsController();
        var request = new CommentEditRequest { Body = "Upravená" };

        var result = await controller.EditActComment(writer, caseId, actId, commentId, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.UpdateCaseId, Is.EqualTo(caseId));
            Assert.That(writer.UpdateActId, Is.EqualTo(actId));
            Assert.That(writer.UpdateCommentId, Is.EqualTo(commentId));
            Assert.That(writer.UpdateRequest, Is.SameAs(request));
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }
    }

    [Test]
    public async Task EditingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = new RecordingCommentWriter { UpdateOutcome = CommentWriteOutcome.NotAuthor };
        var controller = new ActCommentsController();

        var result = await controller.EditActComment(
            writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "x" }, CancellationToken.None);

        AssertProblem(result, 403);
    }

    [Test]
    public async Task EditingAMissingNoteIsAProblemWithFourOhFour()
    {
        var writer = new RecordingCommentWriter { UpdateOutcome = CommentWriteOutcome.NotFound };
        var controller = new ActCommentsController();

        var result = await controller.EditActComment(
            writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "x" }, CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task AnEditOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = new RecordingCommentWriter { UpdateOutcome = (CommentWriteOutcome)99 };
        var controller = new ActCommentsController();

        await Assert.ThatAsync(
            () => controller.EditActComment(
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
        var writer = new RecordingCommentWriter { DeleteOutcome = CommentWriteOutcome.Written };
        var controller = new ActCommentsController();

        var result = await controller.DeleteActComment(writer, caseId, actId, commentId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.DeleteCaseId, Is.EqualTo(caseId));
            Assert.That(writer.DeleteActId, Is.EqualTo(actId));
            Assert.That(writer.DeleteCommentId, Is.EqualTo(commentId));
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }
    }

    [Test]
    public async Task DeletingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = new RecordingCommentWriter { DeleteOutcome = CommentWriteOutcome.NotAuthor };
        var controller = new ActCommentsController();

        var result = await controller.DeleteActComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 403);
    }

    [Test]
    public async Task DeletingAMissingNoteIsAProblemWithFourOhFour()
    {
        var writer = new RecordingCommentWriter { DeleteOutcome = CommentWriteOutcome.NotFound };
        var controller = new ActCommentsController();

        var result = await controller.DeleteActComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    private static ProblemDetails AssertProblem(IActionResult? result, in int statusCode)
    {
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(objectResult.StatusCode, Is.EqualTo(statusCode));
            Assert.That(objectResult.Value, Is.InstanceOf<ProblemDetails>());
        }

        return (ProblemDetails)objectResult.Value!;
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
