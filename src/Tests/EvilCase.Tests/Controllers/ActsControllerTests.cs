using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ActsControllerTests
{
    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = ListingReader([Item("Podání"), Item("Rozhodnutí")]);
        var controller = new ActsController();

        var response = await controller.ListCaseActs(reader, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(response.Items.Select(static item => item.Title), Is.EqualTo(["Podání", "Rozhodnutí"]));
    }

    [Test]
    public async Task TheCaseIdInTheRouteReachesTheReader()
    {
        var caseId = Guid.CreateVersion7();
        var reader = Substitute.For<IActReader>();
        var controller = new ActsController();

        await controller.ListCaseActs(reader, caseId, CancellationToken.None);

        await reader
            .Received(1)
            .ListCaseActs(caseId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheListRequestReachesTheReaderUntouched()
    {
        ActListRequest? listRequest = null;
        var reader = Substitute.For<IActReader>();
        reader
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns([])
            .AndDoes(call => listRequest = call.Arg<ActListRequest>());
        var controller = new ActsController();

        await controller.ListActs(reader, new ActListRequest { Take = 5 }, CancellationToken.None);

        Assert.That(listRequest?.Take, Is.EqualTo(5), "the controller decides nothing about the cap");
    }

    [Test]
    public async Task TheActsAcrossEveryCaseComeBackInTheOrderTheReaderGaveThem()
    {
        var reader = ListingReader([Item("druhý"), Item("první")]);
        var controller = new ActsController();

        var response = await controller.ListActs(reader, new ActListRequest(), CancellationToken.None);

        Assert.That(response.Items.Select(static item => item.Title), Is.EqualTo(["druhý", "první"]), "the controller does not re-order what the reader gave it");
    }

    [Test]
    public async Task TheRequestAndTheCaseIdReachTheWriterUntouched()
    {
        var caseId = Guid.CreateVersion7();
        var writer = CreatingWriter(new ActCreateResult { Outcome = ActCreateOutcome.Created, Act = Item("Podání") });
        var controller = new ActsController();
        var request = Request();

        await controller.CreateAct(writer, caseId, request, CancellationToken.None);

        await writer
            .Received(1)
            .CreateAct(caseId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheCreatedActIsWhatTheWriterReturned()
    {
        var created = Item("Podání");
        var writer = CreatingWriter(new ActCreateResult { Outcome = ActCreateOutcome.Created, Act = created });
        var controller = new ActsController();

        var result = await controller.CreateAct(writer, Guid.CreateVersion7(), Request(), CancellationToken.None);

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
        var caseId = Guid.CreateVersion7();

        var response = await controller.CreateAct(writer, caseId, Request(), CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)response.Result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(201), "a POST that creates a row answers 201 Created");
            Assert.That(result.ActionName, Is.EqualTo(nameof(ActsController.GetAct)), "the Location names the detail action of the act");
            Assert.That(result.RouteValues?["caseId"], Is.EqualTo(caseId), "the Location carries the case the act was filed into");
            Assert.That(result.RouteValues?["actId"], Is.EqualTo(act.ActId), "the Location carries the id of the act that was filed");
        }
    }

    [Test]
    public async Task FilingIntoACaseThatIsNotThereIsAProblemWithFourOhFour()
    {
        var writer = CreatingWriter(new ActCreateResult { Outcome = ActCreateOutcome.CaseNotFound });
        var controller = new ActsController();

        var result = await controller.CreateAct(writer, Guid.CreateVersion7(), Request(), CancellationToken.None);

        AssertProblem(result.Result, 404);
    }

    [Test]
    public async Task FilingWithAContactThatIsNotThereIsAConflict()
    {
        var writer = CreatingWriter(new ActCreateResult { Outcome = ActCreateOutcome.ContactNotFound });
        var controller = new ActsController();

        var result = await controller.CreateAct(writer, Guid.CreateVersion7(), Request(), CancellationToken.None);

        var problem = AssertProblem(result.Result, 409);

        Assert.That(problem.Title, Is.EqualTo(ContactProblems.UnknownContact), "an id named in the body is a conflict, not a 404");
    }

    [Test]
    public async Task TheDetailIsAskedForBothIdsInTheRoute()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var reader = DetailReader(Detail());
        var controller = new ActsController();

        await controller.GetAct(reader, caseId, actId, CancellationToken.None);

        await reader
            .Received(1)
            .GetActDetail(caseId, actId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheActDetailIsWhatTheReaderReturned()
    {
        var detail = Detail();
        var reader = DetailReader(detail);
        var controller = new ActsController();

        var result = await controller.GetAct(reader, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

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

        var result = await controller.GetAct(reader, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result.Result, 404);
    }

    [Test]
    public async Task AnEditReachesTheWriterWithBothRouteIdsAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var writer = EditingWriter(ActUpdateOutcome.Updated);
        var controller = new ActsController();
        var request = EditRequest();

        await controller.EditAct(writer, caseId, actId, request, CancellationToken.None);

        await writer
            .Received(1)
            .UpdateAct(caseId, actId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AnEditThatSucceedsAnswersWithNoContent()
    {
        var writer = EditingWriter(ActUpdateOutcome.Updated);
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task EditingAMissingActIsAProblemWithFourOhFour()
    {
        var writer = EditingWriter(ActUpdateOutcome.NotFound);
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNotFound));
    }

    [Test]
    public async Task AnEditNamingAContactThatIsNotThereIsAConflict()
    {
        var writer = EditingWriter(ActUpdateOutcome.ContactNotFound);
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo(ContactProblems.UnknownContact), "an id named in the body is a conflict, not a 404");
    }

    [Test]
    public async Task AnActNumberOutsideTheFormatIsAFieldErrorOnTheNumber()
    {
        var writer = EditingWriter(ActUpdateOutcome.InvalidActNumber);
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

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

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Detail, Is.Not.Null, "a number another act holds is a conflict the user resolves");
    }

    [Test]
    public async Task ADeleteReachesTheWriterWithBothRouteIds()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var writer = DeletingWriter(DeleteOutcome.Deleted);
        var controller = new ActsController();

        await controller.DeleteAct(writer, caseId, actId, CancellationToken.None);

        await writer
            .Received(1)
            .DeleteAct(caseId, actId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ADeleteThatSucceedsAnswersWithNoContent()
    {
        var writer = DeletingWriter(DeleteOutcome.Deleted);
        var controller = new ActsController();

        var result = await controller.DeleteAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAMissingActIsAProblemWithFourOhFour()
    {
        var writer = DeletingWriter(DeleteOutcome.NotFound);
        var controller = new ActsController();

        var result = await controller.DeleteAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNotFound));
    }

    [Test]
    public async Task AnActDeleteOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = DeletingWriter((DeleteOutcome)99);
        var controller = new ActsController();

        await Assert.ThatAsync(
            async () => await controller.DeleteAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None),
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
            ActNumber = "EC/20260821-001/20260825-001",
            Direction = ActDirection.Incoming,
            Title = title,
            Date = new DateOnly(2026, 8, 25),
            ContactName = "Kontakt",
        };
    }

    private static CreateActRequest Request()
    {
        return new CreateActRequest
        {
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
            .ListCaseActs(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(items);
        reader
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(items);

        return reader;
    }

    private static IActReader DetailReader(ActDetail? detail)
    {
        var reader = Substitute.For<IActReader>();
        reader
            .GetActDetail(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(detail);

        return reader;
    }

    private static IActWriter CreatingWriter(ActCreateResult result)
    {
        var writer = Substitute.For<IActWriter>();
        writer
            .CreateAct(Arg.Any<Guid>(), Arg.Any<CreateActRequest>(), Arg.Any<CancellationToken>())
            .Returns(result);

        return writer;
    }

    private static IActWriter EditingWriter(ActUpdateOutcome outcome)
    {
        var writer = Substitute.For<IActWriter>();
        writer
            .UpdateAct(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ActEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static IActWriter DeletingWriter(DeleteOutcome outcome)
    {
        var writer = Substitute.For<IActWriter>();
        writer
            .DeleteAct(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }
}
