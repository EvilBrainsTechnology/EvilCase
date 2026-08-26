using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ActsControllerTests
{
    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingActReader { Items = [Item("Podání"), Item("Rozhodnutí")] };
        var controller = new ActsController();

        var response = await controller.ListActs(reader, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(response.Items.Select(item => item.Title), Is.EqualTo(["Podání", "Rozhodnutí"]));
    }

    [Test]
    public async Task TheCaseIdInTheRouteReachesTheReader()
    {
        var caseId = Guid.CreateVersion7();
        var reader = new RecordingActReader();
        var controller = new ActsController();

        await controller.ListActs(reader, caseId, CancellationToken.None);

        Assert.That(reader.CaseId, Is.EqualTo(caseId));
    }

    [Test]
    public async Task TheRequestAndTheCaseIdReachTheWriterUntouched()
    {
        var caseId = Guid.CreateVersion7();
        var writer = new RecordingActWriter { Result = new ActCreateResult { Outcome = ActCreateOutcome.Created, Act = Item("Podání") } };
        var controller = new ActsController();
        var request = Request();

        await controller.CreateAct(writer, caseId, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.CaseId, Is.EqualTo(caseId));
            Assert.That(writer.Request, Is.SameAs(request));
        }
    }

    [Test]
    public async Task TheCreatedActIsWhatTheWriterReturned()
    {
        var created = Item("Podání");
        var writer = new RecordingActWriter { Result = new ActCreateResult { Outcome = ActCreateOutcome.Created, Act = created } };
        var controller = new ActsController();

        var result = await controller.CreateAct(writer, Guid.CreateVersion7(), Request(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Result, Is.InstanceOf<CreatedResult>());
            Assert.That(((CreatedResult)result.Result!).Value, Is.SameAs(created));
        }
    }

    [Test]
    public async Task FilingAnActIsAnsweredWithTwoOhOneCreatedAndTheActsLocation()
    {
        var act = Item("Podání");
        var writer = new RecordingActWriter { Result = new ActCreateResult { Outcome = ActCreateOutcome.Created, Act = act } };
        var controller = new ActsController();
        var caseId = Guid.CreateVersion7();

        var result = await controller.CreateAct(writer, caseId, Request(), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<CreatedResult>());
        var created = (CreatedResult)result.Result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.StatusCode, Is.EqualTo(201), "a POST that creates a row answers 201 Created");
            Assert.That(
                created.Location,
                Is.EqualTo($"/api/cases/{caseId}/acts/{act.Id}"),
                "the Location names the act's own detail route");
        }
    }

    [Test]
    public async Task FilingIntoACaseThatIsNotThereIsAProblemWithFourOhFour()
    {
        var writer = new RecordingActWriter { Result = new ActCreateResult { Outcome = ActCreateOutcome.CaseNotFound } };
        var controller = new ActsController();

        var result = await controller.CreateAct(writer, Guid.CreateVersion7(), Request(), CancellationToken.None);

        AssertProblem(result.Result, 404);
    }

    [Test]
    public async Task FilingWithAContactThatIsNotThereIsAProblemWithFourOhFour()
    {
        var caseNotFound = new RecordingActWriter { Result = new ActCreateResult { Outcome = ActCreateOutcome.CaseNotFound } };
        var contactNotFound = new RecordingActWriter { Result = new ActCreateResult { Outcome = ActCreateOutcome.ContactNotFound } };
        var controller = new ActsController();

        var caseResult = await controller.CreateAct(caseNotFound, Guid.CreateVersion7(), Request(), CancellationToken.None);
        var contactResult = await controller.CreateAct(contactNotFound, Guid.CreateVersion7(), Request(), CancellationToken.None);

        var caseProblem = AssertProblem(caseResult.Result, 404);
        var contactProblem = AssertProblem(contactResult.Result, 404);

        Assert.That(contactProblem.Title, Is.Not.EqualTo(caseProblem.Title), "the answer says which id was not found");
    }

    [Test]
    public async Task TheDetailIsAskedForBothIdsInTheRoute()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var reader = new RecordingActReader { DetailResult = Detail() };
        var controller = new ActsController();

        await controller.GetAct(reader, caseId, actId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reader.DetailCaseId, Is.EqualTo(caseId));
            Assert.That(reader.DetailActId, Is.EqualTo(actId));
        }
    }

    [Test]
    public async Task TheActDetailIsWhatTheReaderReturned()
    {
        var detail = Detail();
        var reader = new RecordingActReader { DetailResult = detail };
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
        var reader = new RecordingActReader { DetailResult = null };
        var controller = new ActsController();

        var result = await controller.GetAct(reader, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result.Result, 404);
    }

    [Test]
    public async Task AnEditReachesTheWriterWithBothRouteIdsAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var writer = new RecordingActWriter { UpdateOutcome = ActUpdateOutcome.Updated };
        var controller = new ActsController();
        var request = EditRequest();

        await controller.EditAct(writer, caseId, actId, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.UpdateCaseId, Is.EqualTo(caseId));
            Assert.That(writer.UpdateActId, Is.EqualTo(actId));
            Assert.That(writer.UpdateRequest, Is.SameAs(request));
        }
    }

    [Test]
    public async Task AnEditThatSucceedsAnswersWithNoContent()
    {
        var writer = new RecordingActWriter { UpdateOutcome = ActUpdateOutcome.Updated };
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task EditingAMissingActIsAProblemWithFourOhFour()
    {
        var writer = new RecordingActWriter { UpdateOutcome = ActUpdateOutcome.NotFound };
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNotFound));
    }

    [Test]
    public async Task AnEditNamingAContactThatIsNotThereIsAProblemWithFourOhFour()
    {
        var writer = new RecordingActWriter { UpdateOutcome = ActUpdateOutcome.ContactNotFound };
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.Not.EqualTo(ActProblems.ActNotFound), "the answer says which id was not found");
    }

    [Test]
    public async Task AnActNumberOutsideTheFormatIsAFieldErrorOnTheNumber()
    {
        var writer = new RecordingActWriter { UpdateOutcome = ActUpdateOutcome.InvalidActNumber };
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
        var writer = new RecordingActWriter { UpdateOutcome = ActUpdateOutcome.ActNumberTaken };
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNumberTaken));
            Assert.That(problem.Detail, Is.Not.Null);
        }
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

    private static ActListItem Item(string title)
    {
        return new()
        {
            Id = Guid.CreateVersion7(),
            ActNumber = "EC/20260821-001/20260825-001",
            Direction = ActDirection.Incoming,
            Title = title,
            Date = new DateOnly(2026, 8, 25),
            IssuedByName = "Odesílatel",
        };
    }

    private static CreateActRequest Request()
    {
        return new CreateActRequest
        {
            Direction = ActDirection.Incoming,
            Date = new DateOnly(2026, 8, 25),
            Title = "Podání",
            IssuedByContactId = Guid.CreateVersion7(),
        };
    }

    private static ActDetail Detail()
    {
        return new ActDetail
        {
            Id = Guid.CreateVersion7(),
            CaseNumber = "EC/20260821-001",
            ActNumber = "EC/20260821-001/20260825-001",
            Direction = ActDirection.Incoming,
            Date = new DateOnly(2026, 8, 25),
            Title = "Podání",
            IssuedByContact = new ContactListItem { Id = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Odesílatel" },
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
            IssuedByContactId = Guid.CreateVersion7(),
        };
    }

    private sealed class RecordingActReader : IActReader
    {
        public Guid? CaseId { get; private set; }

        public Guid? DetailCaseId { get; private set; }

        public Guid? DetailActId { get; private set; }

        public IReadOnlyList<ActListItem> Items { get; init; } = [];

        public ActDetail? DetailResult { get; init; }

        public Task<IReadOnlyList<ActListItem>> ListActs(Guid caseId, CancellationToken token)
        {
            this.CaseId = caseId;

            return Task.FromResult(this.Items);
        }

        public Task<ActDetail?> GetActDetail(Guid caseId, Guid actId, CancellationToken token)
        {
            this.DetailCaseId = caseId;
            this.DetailActId = actId;

            return Task.FromResult(this.DetailResult);
        }
    }

    private sealed class RecordingActWriter : IActWriter
    {
        public Guid? CaseId { get; private set; }

        public CreateActRequest? Request { get; private set; }

        public Guid? UpdateCaseId { get; private set; }

        public Guid? UpdateActId { get; private set; }

        public ActEditRequest? UpdateRequest { get; private set; }

        public ActCreateResult Result { get; init; } = new() { Outcome = ActCreateOutcome.Created, Act = Item("Podání") };

        public ActUpdateOutcome UpdateOutcome { get; init; }

        public Task<ActCreateResult> CreateAct(Guid caseId, CreateActRequest request, CancellationToken token)
        {
            this.CaseId = caseId;
            this.Request = request;

            return Task.FromResult(this.Result);
        }

        public Task<ActUpdateOutcome> UpdateAct(Guid caseId, Guid actId, ActEditRequest request, CancellationToken token)
        {
            this.UpdateCaseId = caseId;
            this.UpdateActId = actId;
            this.UpdateRequest = request;

            return Task.FromResult(this.UpdateOutcome);
        }
    }
}
