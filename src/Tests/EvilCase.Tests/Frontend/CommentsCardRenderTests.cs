using Bunit;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.App.Components;
using Microsoft.Extensions.DependencyInjection;
using TabBlazor.Services;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CommentsCardRenderTests
{
    [Test]
    public void TheCardNamesWhatItBelongsTo()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        var component = ctx.Render<CommentsCard>(static parameters => parameters
            .Add(static card => card.Title, "Komentáře spisu")
            .Add(static card => card.Placeholder, "Poznámka ke spisu…")
            .Add(static card => card.LoadComments, static (_) => Task.FromResult<IReadOnlyList<CommentItem>>([]))
            .Add(static card => card.AddComment, static (_, _) => Task.CompletedTask)
            .Add(static card => card.SaveComment, static (_, _, _) => Task.CompletedTask)
            .Add(static card => card.DeleteComment, static (_, _) => Task.CompletedTask));

        Assert.That(component.Find(".ec-card-title").TextContent.Trim(), Is.EqualTo("Komentáře spisu"));
    }

    [Test]
    public void AnEmptyListShowsTheEmptyState()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        var component = ctx.Render<CommentsCard>(static parameters => parameters
            .Add(static card => card.Title, "Komentáře spisu")
            .Add(static card => card.Placeholder, "Poznámka ke spisu…")
            .Add(static card => card.LoadComments, static (_) => Task.FromResult<IReadOnlyList<CommentItem>>([]))
            .Add(static card => card.AddComment, static (_, _) => Task.CompletedTask)
            .Add(static card => card.SaveComment, static (_, _, _) => Task.CompletedTask)
            .Add(static card => card.DeleteComment, static (_, _) => Task.CompletedTask));

        Assert.That(component.Find(".ec-empty-text").TextContent, Is.EqualTo("Zatím tu nejsou žádné komentáře."));
    }

    [Test]
    public void WritingACommentSendsItAndReloadsTheList()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        var loads = 0;
        CommentEditRequest? sent = null;

        var component = ctx.Render<CommentsCard>(parameters => parameters
            .Add(static card => card.Title, "Komentáře spisu")
            .Add(static card => card.Placeholder, "Poznámka ke spisu…")
            .Add(
                static card => card.LoadComments,
                _ =>
                {
                    loads++;

                    return Task.FromResult<IReadOnlyList<CommentItem>>([]);
                })
            .Add(
                static card => card.AddComment,
                (request, _) =>
                {
                    sent = request;

                    return Task.CompletedTask;
                })
            .Add(static card => card.SaveComment, static (_, _, _) => Task.CompletedTask)
            .Add(static card => card.DeleteComment, static (_, _) => Task.CompletedTask));

        component.Find("#comment-new-body").Change("Doručeno datovou schránkou.");
        component.Find("#comment-new button").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sent?.Body, Is.EqualTo("Doručeno datovou schránkou."));
            Assert.That(loads, Is.EqualTo(2));
            Assert.That(component.Find("#comment-new-body").GetAttribute("value"), Is.Null.Or.Empty, "a sent comment clears the composer");
        }
    }

    [Test]
    public void AnEmptyBodyIsRefusedAndNothingIsSent()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        var sends = 0;

        var component = ctx.Render<CommentsCard>(parameters => parameters
            .Add(static card => card.Title, "Komentáře spisu")
            .Add(static card => card.Placeholder, "Poznámka ke spisu…")
            .Add(static card => card.LoadComments, static (_) => Task.FromResult<IReadOnlyList<CommentItem>>([]))
            .Add(
                static card => card.AddComment,
                (_, _) =>
                {
                    sends++;

                    return Task.CompletedTask;
                })
            .Add(static card => card.SaveComment, static (_, _, _) => Task.CompletedTask)
            .Add(static card => card.DeleteComment, static (_, _) => Task.CompletedTask));

        component.Find("#comment-new button").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sends, Is.Zero, "AddComment was never called");
            Assert.That(component.Find(".ec-field-error").TextContent, Does.Contain("Zadejte text komentáře"));
        }
    }

    [Test]
    public void OnlyTheAuthorGetsTheEditAndDeleteButtons()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        IReadOnlyList<CommentItem> items =
        [
            new CommentItem { CommentId = Guid.NewGuid(), Body = "A", AuthorEmail = "a@vzorov.cz", IsAuthor = true, Created = DateTime.UtcNow },
            new CommentItem { CommentId = Guid.NewGuid(), Body = "B", AuthorEmail = "b@vzorov.cz", IsAuthor = false, Created = DateTime.UtcNow },
        ];

        var component = ctx.Render<CommentsCard>(parameters => parameters
            .Add(static card => card.Title, "Komentáře spisu")
            .Add(static card => card.Placeholder, "Poznámka ke spisu…")
            .Add(static card => card.LoadComments, _ => Task.FromResult(items))
            .Add(static card => card.AddComment, static (_, _) => Task.CompletedTask)
            .Add(static card => card.SaveComment, static (_, _, _) => Task.CompletedTask)
            .Add(static card => card.DeleteComment, static (_, _) => Task.CompletedTask));

        Assert.That(component.FindAll(".ec-comment-actions"), Has.Count.EqualTo(1));
    }

    [Test]
    public void EditingACommentSendsTheNewBody()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        var commentId = Guid.NewGuid();
        IReadOnlyList<CommentItem> items = [new CommentItem { CommentId = commentId, Body = "Původní", AuthorEmail = "a@vzorov.cz", IsAuthor = true, Created = DateTime.UtcNow }];

        Guid? savedId = null;
        string? savedBody = null;

        var component = ctx.Render<CommentsCard>(parameters => parameters
            .Add(static card => card.Title, "Komentáře spisu")
            .Add(static card => card.Placeholder, "Poznámka ke spisu…")
            .Add(static card => card.LoadComments, _ => Task.FromResult(items))
            .Add(static card => card.AddComment, static (_, _) => Task.CompletedTask)
            .Add(
                static card => card.SaveComment,
                (id, request, _) =>
                {
                    savedId = id;
                    savedBody = request.Body;

                    return Task.CompletedTask;
                })
            .Add(static card => card.DeleteComment, static (_, _) => Task.CompletedTask));

        component.Find(".ec-comment-actions button:first-child").Click();
        component.Find("#comment-edit-body").Change("Opraveno.");
        component.Find("#comment-edit button:last-child").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(savedId, Is.EqualTo(commentId));
            Assert.That(savedBody, Is.EqualTo("Opraveno."));
        }
    }

    [Test]
    public void TheAvatarShowsTheAuthorsInitials()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        IReadOnlyList<CommentItem> items = [new CommentItem { CommentId = Guid.NewGuid(), Body = "A", AuthorEmail = "martin.volek@vzorov.cz", IsAuthor = false, Created = DateTime.UtcNow }];

        var component = ctx.Render<CommentsCard>(parameters => parameters
            .Add(static card => card.Title, "Komentáře spisu")
            .Add(static card => card.Placeholder, "Poznámka ke spisu…")
            .Add(static card => card.LoadComments, _ => Task.FromResult(items))
            .Add(static card => card.AddComment, static (_, _) => Task.CompletedTask)
            .Add(static card => card.SaveComment, static (_, _, _) => Task.CompletedTask)
            .Add(static card => card.DeleteComment, static (_, _) => Task.CompletedTask));

        Assert.That(component.Find(".ec-comment-avatar").TextContent.Trim(), Is.EqualTo("MV"));
    }

    [Test]
    public void AnEditedCommentSaysWhenItWasEdited()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        IReadOnlyList<CommentItem> items =
        [
            new CommentItem
            {
                CommentId = Guid.NewGuid(),
                Body = "A",
                AuthorEmail = "a@vzorov.cz",
                IsAuthor = false,
                Created = new DateTime(2025, 9, 1, 10, 0, 0, DateTimeKind.Utc),
                Updated = new DateTime(2025, 9, 2, 10, 0, 0, DateTimeKind.Utc),
            },
        ];

        var component = ctx.Render<CommentsCard>(parameters => parameters
            .Add(static card => card.Title, "Komentáře spisu")
            .Add(static card => card.Placeholder, "Poznámka ke spisu…")
            .Add(static card => card.LoadComments, _ => Task.FromResult(items))
            .Add(static card => card.AddComment, static (_, _) => Task.CompletedTask)
            .Add(static card => card.SaveComment, static (_, _, _) => Task.CompletedTask)
            .Add(static card => card.DeleteComment, static (_, _) => Task.CompletedTask));

        Assert.That(component.Find(".ec-comment-meta").TextContent, Does.Contain("upraveno"));
    }
}
