using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ActsControllerTests
{
    [Test]
    public async Task TheListRequestReachesTheReaderUntouched()
    {
        var caseId = Guid.CreateVersion7();
        ActListRequest? listRequest = null;
        var reader = Substitute.For<IActReader>();
        reader
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ActListResponse { Items = [], TotalCount = 0 })
            .AndDoes(call => listRequest = call.Arg<ActListRequest>());
        var controller = new ActsController();

        await controller.ListActs(reader, new ActListRequest { Take = 5, Skip = 10, CaseId = caseId }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(listRequest?.Take, Is.EqualTo(5), "the controller decides nothing about the page");
            Assert.That(listRequest?.Skip, Is.EqualTo(10), "the controller decides nothing about the page");
            Assert.That(listRequest?.CaseId, Is.EqualTo(caseId), "the case a list is narrowed to rides in the request, not in the route");
        }
    }

    [Test]
    public async Task TheActsAcrossEveryCaseComeBackInTheOrderTheReaderGaveThem()
    {
        var reader = ListingReader([Item("druhý"), Item("první")]);
        var controller = new ActsController();

        var response = await controller.ListActs(reader, new ActListRequest { Take = 20 }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Items.Select(static item => item.Title), Is.EqualTo(["druhý", "první"]), "the controller does not re-order what the reader gave it");
            Assert.That(response.TotalCount, Is.EqualTo(9), "the controller hands the total through untouched");
        }
    }

    [Test]
    public async Task TheRequestReachesTheWriterUntouched()
    {
        var writer = CreatingWriter(new ActCreateResult { Outcome = ActCreateOutcome.Created, Act = Item("Podání") });
        var controller = new ActsController();
        var request = Request();

        await controller.CreateAct(writer, request, CancellationToken.None);

        await writer
            .Received(1)
            .CreateAct(request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheCreatedActIsWhatTheWriterReturned()
    {
        var created = Item("Podání");
        var writer = CreatingWriter(new ActCreateResult { Outcome = ActCreateOutcome.Created, Act = created });
        var controller = new ActsController();

        var result = await controller.CreateAct(writer, Request(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            Assert.That(((CreatedAtActionResult)result.Result!).Value, Is.SameAs(created));
        }
    }

    [Test]
    public async Task AFiledActIsAnsweredWithCreatedAtItsDetailRoute()
    {
        var act = Item("Podání");
        var writer = CreatingWriter(new ActCreateResult { Outcome = ActCreateOutcome.Created, Act = act });
        var controller = new ActsController();

        var response = await controller.CreateAct(writer, Request(), CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)response.Result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(201), "a POST that creates a row answers 201 Created");
            Assert.That(result.ActionName, Is.EqualTo(nameof(ActsController.GetAct)), "the Location names the detail action of the act");
            Assert.That(result.RouteValues, Does.Not.ContainKey("caseId"), "an act is addressed by its own id alone");
            Assert.That(result.RouteValues?["actId"], Is.EqualTo(act.ActId), "the Location carries the id of the act that was filed");
        }
    }

    [Test]
    public void FilingPostsToTheActsRouteWithTheCaseInTheBody()
    {
        var method = typeof(ActsController).GetMethod(nameof(ActsController.CreateAct))!;
        var post = method.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false).Cast<HttpPostAttribute>().Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(post.Template, Is.EqualTo("acts"), "an act is filed on the flat route, not under a case");
            Assert.That(method.GetParameters().Select(static parameter => parameter.Name), Does.Not.Contain("caseId"), "the case rides in the body");
        }
    }

    [Test]
    public async Task FilingIntoACaseThatIsNotThereIsAProblemWithFourOhFour()
    {
        var writer = CreatingWriter(new ActCreateResult { Outcome = ActCreateOutcome.CaseNotFound });
        var controller = new ActsController();

        var result = await controller.CreateAct(writer, Request(), CancellationToken.None);

        AssertProblem(result.Result, 404);
    }

    [Test]
    public async Task FilingWithAContactThatIsNotThereIsAConflict()
    {
        var writer = CreatingWriter(new ActCreateResult { Outcome = ActCreateOutcome.ContactNotFound });
        var controller = new ActsController();

        var result = await controller.CreateAct(writer, Request(), CancellationToken.None);

        var problem = AssertProblem(result.Result, 409);

        Assert.That(problem.Title, Is.EqualTo(ContactProblems.UnknownContact), "an id named in the body is a conflict, not a 404");
    }

    [Test]
    public async Task TheDetailIsAskedForTheIdInTheRoute()
    {
        var actId = Guid.CreateVersion7();
        var reader = DetailReader(Detail());
        var controller = new ActsController();

        await controller.GetAct(reader, actId, CancellationToken.None);

        await reader
            .Received(1)
            .GetActDetail(actId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheActDetailIsWhatTheReaderReturned()
    {
        var detail = Detail();
        var reader = DetailReader(detail);
        var controller = new ActsController();

        var result = await controller.GetAct(reader, Guid.CreateVersion7(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(detail));
        }
    }

    [Test]
    public async Task AMissingActIsAProblemWithFourOhFour()
    {
        var reader = DetailReader(detail: null);
        var controller = new ActsController();

        var result = await controller.GetAct(reader, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result.Result, 404);
    }

    [Test]
    public async Task AnEditReachesTheWriterWithTheRouteIdAndTheBody()
    {
        var actId = Guid.CreateVersion7();
        var writer = EditingWriter(ActUpdateOutcome.Updated);
        var controller = new ActsController();
        var request = EditRequest();

        await controller.EditAct(writer, actId, request, CancellationToken.None);

        await writer
            .Received(1)
            .UpdateAct(actId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AnEditThatSucceedsAnswersWithNoContent()
    {
        var writer = EditingWriter(ActUpdateOutcome.Updated);
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task EditingAMissingActIsAProblemWithFourOhFour()
    {
        var writer = EditingWriter(ActUpdateOutcome.NotFound);
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNotFound));
    }

    [Test]
    public async Task AnEditNamingAContactThatIsNotThereIsAConflict()
    {
        var writer = EditingWriter(ActUpdateOutcome.ContactNotFound);
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo(ContactProblems.UnknownContact), "an id named in the body is a conflict, not a 404");
    }

    [Test]
    public async Task AnActNumberOutsideTheFormatIsAFieldErrorOnTheNumber()
    {
        var writer = EditingWriter(ActUpdateOutcome.InvalidActNumber);
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var problem = (ObjectResult)result;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.StatusCode, Is.EqualTo(400));
            Assert.That(problem.Value, Is.InstanceOf<ValidationProblemDetails>());
        }

        var validation = (ValidationProblemDetails)problem.Value!;

        Assert.That(
            validation.Errors,
            Does.ContainKey(nameof(ActEditRequest.ActNumber)),
            "a hand-written number outside the format is reported on the field that carries it");
    }

    [Test]
    public async Task AnActNumberAnotherActHoldsIsAConflict()
    {
        var writer = EditingWriter(ActUpdateOutcome.ActNumberTaken);
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Detail, Is.Not.Null, "a number another act holds is a conflict the user resolves");
    }

    [Test]
    public async Task ADeleteReachesTheWriterWithTheRouteId()
    {
        var actId = Guid.CreateVersion7();
        var writer = DeletingWriter(DeleteOutcome.Deleted);
        var controller = new ActsController();

        await controller.DeleteAct(writer, actId, CancellationToken.None);

        await writer
            .Received(1)
            .DeleteAct(actId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ADeleteThatSucceedsAnswersWithNoContent()
    {
        var writer = DeletingWriter(DeleteOutcome.Deleted);
        var controller = new ActsController();

        var result = await controller.DeleteAct(writer, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAMissingActIsAProblemWithFourOhFour()
    {
        var writer = DeletingWriter(DeleteOutcome.NotFound);
        var controller = new ActsController();

        var result = await controller.DeleteAct(writer, Guid.CreateVersion7(), CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNotFound));
    }

    [Test]
    public async Task AnActDeleteOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = DeletingWriter((DeleteOutcome)99);
        var controller = new ActsController();

        await Assert.ThatAsync(
            async () => await controller.DeleteAct(writer, Guid.CreateVersion7(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    private static ActListItem Item(string title)
    {
        return new()
        {
            ActId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260821-001",
            CaseTitle = "Přestupek",
            ActNumber = "EC/20260821-001/20260825-001",
            Direction = ActDirection.Incoming,
            Title = title,
            Date = new DateOnly(2026, 8, 25),
            Changed = new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc),
            ContactName = "Kontakt",
        };
    }

    private static CreateActRequest Request()
    {
        return new CreateActRequest
        {
            CaseId = Guid.CreateVersion7(),
            Direction = ActDirection.Incoming,
            Date = new DateOnly(2026, 8, 25),
            Title = "Podání",
            ContactId = Guid.CreateVersion7(),
        };
    }

    private static ActDetail Detail()
    {
        return new ActDetail
        {
            ActId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260821-001",
            CaseTitle = "Vzorový spis",
            CaseStatus = CaseStatus.Active,
            ActNumber = "EC/20260821-001/20260825-001",
            Direction = ActDirection.Incoming,
            Date = new DateOnly(2026, 8, 25),
            Title = "Podání",
            Contact = new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Kontakt" },
        };
    }

    private static ActEditRequest EditRequest()
    {
        return new ActEditRequest
        {
            ActNumber = "EC/20260821-001/20260825-001",
            Direction = ActDirection.Incoming,
            Date = new DateOnly(2026, 8, 25),
            Title = "Podání",
            ContactId = Guid.CreateVersion7(),
        };
    }

    private static IActReader ListingReader(IReadOnlyList<ActListItem> items)
    {
        var reader = Substitute.For<IActReader>();
        reader
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ActListResponse { Items = items, TotalCount = 9 });

        return reader;
    }

    private static IActReader DetailReader(ActDetail? detail)
    {
        var reader = Substitute.For<IActReader>();
        reader
            .GetActDetail(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(detail);

        return reader;
    }

    private static IActWriter CreatingWriter(ActCreateResult result)
    {
        var writer = Substitute.For<IActWriter>();
        writer
            .CreateAct(Arg.Any<CreateActRequest>(), Arg.Any<CancellationToken>())
            .Returns(result);

        return writer;
    }

    private static IActWriter EditingWriter(ActUpdateOutcome outcome)
    {
        var writer = Substitute.For<IActWriter>();
        writer
            .UpdateAct(Arg.Any<Guid>(), Arg.Any<ActEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static IActWriter DeletingWriter(DeleteOutcome outcome)
    {
        var writer = Substitute.For<IActWriter>();
        writer
            .DeleteAct(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }
}
