using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Domain.Acts;
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

        Assert.That(result.Value, Is.SameAs(created));
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

    private sealed class RecordingActReader : IActReader
    {
        public Guid? CaseId { get; private set; }

        public IReadOnlyList<ActListItem> Items { get; init; } = [];

        public Task<IReadOnlyList<ActListItem>> ListActs(Guid caseId, CancellationToken token)
        {
            this.CaseId = caseId;

            return Task.FromResult(this.Items);
        }
    }

    private sealed class RecordingActWriter : IActWriter
    {
        public Guid? CaseId { get; private set; }

        public CreateActRequest? Request { get; private set; }

        public required ActCreateResult Result { get; init; }

        public Task<ActCreateResult> CreateAct(Guid caseId, CreateActRequest request, CancellationToken token)
        {
            this.CaseId = caseId;
            this.Request = request;

            return Task.FromResult(this.Result);
        }
    }
}
