using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Comments;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CaseCommentsControllerTests
{
    [Test]
    public async Task TheListIsAskedForTheCaseInTheRoute()
    {
        var caseId = Guid.CreateVersion7();
        var reader = new RecordingCommentReader();
        var controller = new CaseCommentsController();

        await controller.ListCaseComments(reader, caseId, CancellationToken.None);

        Assert.That(reader.CaseId, Is.EqualTo(caseId));
    }

    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingCommentReader { Items = [Item("První"), Item("Druhá")] };
        var controller = new CaseCommentsController();

        var response = await controller.ListCaseComments(reader, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(response.Items.Select(item => item.Body), Is.EqualTo(["První", "Druhá"]));
    }

    [Test]
    public async Task AddingReachesTheWriterWithTheRouteIdAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var writer = new RecordingCommentWriter { AddOutcome = CommentWriteOutcome.Written };
        var controller = new CaseCommentsController();
        var request = new CommentEditRequest { Body = "Poznámka" };

        var result = await controller.AddCaseComment(writer, caseId, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.AddCaseId, Is.EqualTo(caseId));
            Assert.That(writer.AddRequest, Is.SameAs(request));
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }
    }

    [Test]
    public async Task AddingToAMissingCaseIsAProblemWithFourOhFour()
    {
        var writer = new RecordingCommentWriter { AddOutcome = CommentWriteOutcome.NotFound };
        var controller = new CaseCommentsController();

        var result = await controller.AddCaseComment(writer, Guid.CreateVersion7(), new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.EqualTo("Case not found"), "the answer names the case, not the comment");
    }

    [Test]
    public async Task EditingReachesTheWriterWithBothIdsAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var commentId = Guid.CreateVersion7();
        var writer = new RecordingCommentWriter { UpdateOutcome = CommentWriteOutcome.Written };
        var controller = new CaseCommentsController();
        var request = new CommentEditRequest { Body = "Upravená" };

        var result = await controller.EditCaseComment(writer, caseId, commentId, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.UpdateCaseId, Is.EqualTo(caseId));
            Assert.That(writer.UpdateCommentId, Is.EqualTo(commentId));
            Assert.That(writer.UpdateRequest, Is.SameAs(request));
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }
    }

    [Test]
    public async Task EditingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = new RecordingCommentWriter { UpdateOutcome = CommentWriteOutcome.NotAuthor };
        var controller = new CaseCommentsController();

        var result = await controller.EditCaseComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "x" }, CancellationToken.None);

        AssertProblem(result, 403);
    }

    [Test]
    public async Task EditingAMissingNoteIsAProblemWithFourOhFour()
    {
        var writer = new RecordingCommentWriter { UpdateOutcome = CommentWriteOutcome.NotFound };
        var controller = new CaseCommentsController();

        var result = await controller.EditCaseComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), new CommentEditRequest { Body = "x" }, CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task DeletingReachesTheWriterWithBothIds()
    {
        var caseId = Guid.CreateVersion7();
        var commentId = Guid.CreateVersion7();
        var writer = new RecordingCommentWriter { DeleteOutcome = CommentWriteOutcome.Written };
        var controller = new CaseCommentsController();

        var result = await controller.DeleteCaseComment(writer, caseId, commentId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.DeleteCaseId, Is.EqualTo(caseId));
            Assert.That(writer.DeleteCommentId, Is.EqualTo(commentId));
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }
    }

    [Test]
    public async Task DeletingSomeoneElsesNoteIsAProblemWithFourOhThree()
    {
        var writer = new RecordingCommentWriter { DeleteOutcome = CommentWriteOutcome.NotAuthor };
        var controller = new CaseCommentsController();

        var result = await controller.DeleteCaseComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 403);
    }

    [Test]
    public async Task DeletingAMissingNoteIsAProblemWithFourOhFour()
    {
        var writer = new RecordingCommentWriter { DeleteOutcome = CommentWriteOutcome.NotFound };
        var controller = new CaseCommentsController();

        var result = await controller.DeleteCaseComment(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

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
            Id = Guid.CreateVersion7(),
            Body = body,
            AuthorEmail = "user@example.com",
            IsAuthor = true,
            Created = DateTime.UtcNow,
        };
    }
}
