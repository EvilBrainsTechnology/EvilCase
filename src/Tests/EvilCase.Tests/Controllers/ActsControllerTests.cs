using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Numbers;
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
        var reader = new RecordingActReader { Items = [Item("Podání"), Item("Rozhodnutí")] };
        var controller = new ActsController();

        var response = await controller.ListCaseActs(reader, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(response.Items.Select(static item => item.Title), Is.EqualTo(["Podání", "Rozhodnutí"]));
    }

    [Test]
    public async Task TheCaseIdInTheRouteReachesTheReader()
    {
        var caseId = Guid.CreateVersion7();
        var reader = new RecordingActReader();
        var controller = new ActsController();

        await controller.ListCaseActs(reader, caseId, CancellationToken.None);

        Assert.That(reader.CaseId, Is.EqualTo(caseId));
    }

    [Test]
    public async Task TheListRequestReachesTheReaderUntouched()
    {
        var reader = new RecordingActReader();
        var controller = new ActsController();

        await controller.ListActs(reader, new ActListRequest { Take = 5 }, CancellationToken.None);

        Assert.That(reader.ListRequest?.Take, Is.EqualTo(5), "the controller decides nothing about the cap");
    }

    [Test]
    public async Task TheActsAcrossEveryCaseComeBackInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingActReader { Items = [Item("druhý"), Item("první")] };
        var controller = new ActsController();

        var response = await controller.ListActs(reader, new ActListRequest(), CancellationToken.None);

        Assert.That(response.Items.Select(static item => item.Title), Is.EqualTo(["druhý", "první"]), "the controller does not re-order what the reader gave it");
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
            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            Assert.That(((CreatedAtActionResult)result.Result!).Value, Is.SameAs(created));
        }
    }

    [Test]
    public async Task AFiledActIsAnsweredWithCreatedAtItsDetailRoute()
    {
        var act = Item("Podání");
        var writer = new RecordingActWriter { Result = new ActCreateResult { Outcome = ActCreateOutcome.Created, Act = act } };
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
        var writer = new RecordingActWriter { Result = new ActCreateResult { Outcome = ActCreateOutcome.CaseNotFound } };
        var controller = new ActsController();

        var result = await controller.CreateAct(writer, Guid.CreateVersion7(), Request(), CancellationToken.None);

        AssertProblem(result.Result, 404);
    }

    [Test]
    public async Task FilingWithAContactThatIsNotThereIsAConflict()
    {
        var writer = new RecordingActWriter { Result = new ActCreateResult { Outcome = ActCreateOutcome.ContactNotFound } };
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
    public async Task AnEditNamingAContactThatIsNotThereIsAConflict()
    {
        var writer = new RecordingActWriter { UpdateOutcome = ActUpdateOutcome.ContactNotFound };
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo(ContactProblems.UnknownContact), "an id named in the body is a conflict, not a 404");
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

        Assert.That(problem.Detail, Is.Not.Null, "a number another act holds is a conflict the user resolves");
    }

    [Test]
    public async Task ADeleteReachesTheWriterWithBothRouteIds()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var writer = new RecordingActWriter { DeleteOutcome = DeleteOutcome.Deleted };
        var controller = new ActsController();

        await controller.DeleteAct(writer, caseId, actId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.DeleteCaseId, Is.EqualTo(caseId));
            Assert.That(writer.DeleteActId, Is.EqualTo(actId));
        }
    }

    [Test]
    public async Task ADeleteThatSucceedsAnswersWithNoContent()
    {
        var writer = new RecordingActWriter { DeleteOutcome = DeleteOutcome.Deleted };
        var controller = new ActsController();

        var result = await controller.DeleteAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAMissingActIsAProblemWithFourOhFour()
    {
        var writer = new RecordingActWriter { DeleteOutcome = DeleteOutcome.NotFound };
        var controller = new ActsController();

        var result = await controller.DeleteAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNotFound));
    }

    [Test]
    public async Task AnActDeleteOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = new RecordingActWriter { DeleteOutcome = (DeleteOutcome)99 };
        var controller = new ActsController();

        await Assert.ThatAsync(
            async () => await controller.DeleteAct(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    [Test]
    public async Task AddingANumberReachesTheWriterWithBothRouteIdsAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var writer = new RecordingExternalActNumberWriter { AddOutcome = ExternalActNumberOutcome.Added };
        var controller = new ActsController();
        var request = Number();

        await controller.AddExternalActNumber(writer, caseId, actId, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.AddCaseId, Is.EqualTo(caseId));
            Assert.That(writer.AddActId, Is.EqualTo(actId));
            Assert.That(writer.AddRequest, Is.SameAs(request), "the controller decides nothing about the number");
        }
    }

    [Test]
    public async Task AddingANumberThatSucceedsAnswersWithNoContent()
    {
        var writer = new RecordingExternalActNumberWriter { AddOutcome = ExternalActNumberOutcome.Added };
        var controller = new ActsController();

        var result = await controller.AddExternalActNumber(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Number(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task AddingANumberToAMissingActIsAProblemWithFourOhFour()
    {
        var writer = new RecordingExternalActNumberWriter { AddOutcome = ExternalActNumberOutcome.ActNotFound };
        var controller = new ActsController();

        var result = await controller.AddExternalActNumber(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Number(), CancellationToken.None);

        var problem = AssertProblem(result, 404);

        Assert.That(problem.Title, Is.EqualTo(ActProblems.ActNotFound));
    }

    [Test]
    public async Task ANumberTheActAlreadyCarriesIsAConflict()
    {
        var writer = new RecordingExternalActNumberWriter { AddOutcome = ExternalActNumberOutcome.ValueTaken };
        var controller = new ActsController();

        var result = await controller.AddExternalActNumber(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Number(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo(ExternalNumberProblems.Taken), "the two conflicts of the add are told apart by the problem title");
    }

    [Test]
    public async Task ANumberNamingAnUnknownContactIsAConflict()
    {
        var writer = new RecordingExternalActNumberWriter { AddOutcome = ExternalActNumberOutcome.UnknownContact };
        var controller = new ActsController();

        var result = await controller.AddExternalActNumber(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Number(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo(ContactProblems.UnknownContact));
    }

    [Test]
    public async Task DeletingANumberAnswersWithNoContent()
    {
        var writer = new RecordingExternalActNumberWriter { DeleteOutcome = DeleteOutcome.Deleted };
        var controller = new ActsController();

        var result = await controller.DeleteExternalActNumber(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingANumberReachesTheWriterWithAllThreeRouteIds()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var numberId = Guid.CreateVersion7();
        var writer = new RecordingExternalActNumberWriter { DeleteOutcome = DeleteOutcome.Deleted };
        var controller = new ActsController();

        await controller.DeleteExternalActNumber(writer, caseId, actId, numberId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.DeleteCaseId, Is.EqualTo(caseId));
            Assert.That(writer.DeleteActId, Is.EqualTo(actId));
            Assert.That(writer.DeleteNumberId, Is.EqualTo(numberId));
        }
    }

    [Test]
    public async Task DeletingAMissingNumberIsAProblemWithFourOhFour()
    {
        var writer = new RecordingExternalActNumberWriter { DeleteOutcome = DeleteOutcome.NotFound };
        var controller = new ActsController();

        var result = await controller.DeleteExternalActNumber(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task ANumberDeleteOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = new RecordingExternalActNumberWriter { DeleteOutcome = (DeleteOutcome)99 };
        var controller = new ActsController();

        await Assert.ThatAsync(
            async () => await controller.DeleteExternalActNumber(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    private static ExternalNumberRequest Number()
    {
        return new() { Value = "1 T 45/2026", AssignedByContactId = Guid.CreateVersion7() };
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
            ActId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260821-001",
            ActNumber = "EC/20260821-001/20260825-001",
            Direction = ActDirection.Incoming,
            Date = new DateOnly(2026, 8, 25),
            Title = "Podání",
            IssuedByContact = new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Odesílatel" },
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

        public ActListRequest? ListRequest { get; private set; }

        public Guid? DetailCaseId { get; private set; }

        public Guid? DetailActId { get; private set; }

        public IReadOnlyList<ActListItem> Items { get; init; } = [];

        public ActDetail? DetailResult { get; init; }

        public async Task<IReadOnlyList<ActListItem>> ListActs(ActListRequest request, CancellationToken token)
        {
            this.ListRequest = request;

            return await Task.FromResult(this.Items);
        }

        public async Task<IReadOnlyList<ActListItem>> ListCaseActs(Guid caseId, CancellationToken token)
        {
            this.CaseId = caseId;

            return await Task.FromResult(this.Items);
        }

        public async Task<ActDetail?> GetActDetail(Guid caseId, Guid actId, CancellationToken token)
        {
            this.DetailCaseId = caseId;
            this.DetailActId = actId;

            return await Task.FromResult(this.DetailResult);
        }
    }

    private sealed class RecordingActWriter : IActWriter
    {
        public Guid? CaseId { get; private set; }

        public CreateActRequest? Request { get; private set; }

        public Guid? UpdateCaseId { get; private set; }

        public Guid? UpdateActId { get; private set; }

        public ActEditRequest? UpdateRequest { get; private set; }

        public Guid? DeleteCaseId { get; private set; }

        public Guid? DeleteActId { get; private set; }

        public ActCreateResult Result { get; init; } = new() { Outcome = ActCreateOutcome.Created, Act = Item("Podání") };

        public ActUpdateOutcome UpdateOutcome { get; init; }

        public DeleteOutcome DeleteOutcome { get; init; }

        public async Task<ActCreateResult> CreateAct(Guid caseId, CreateActRequest request, CancellationToken token)
        {
            this.CaseId = caseId;
            this.Request = request;

            return await Task.FromResult(this.Result);
        }

        public async Task<ActUpdateOutcome> UpdateAct(Guid caseId, Guid actId, ActEditRequest request, CancellationToken token)
        {
            this.UpdateCaseId = caseId;
            this.UpdateActId = actId;
            this.UpdateRequest = request;

            return await Task.FromResult(this.UpdateOutcome);
        }

        public async Task<DeleteOutcome> DeleteAct(Guid caseId, Guid actId, CancellationToken token)
        {
            this.DeleteCaseId = caseId;
            this.DeleteActId = actId;

            return await Task.FromResult(this.DeleteOutcome);
        }
    }

    private sealed class RecordingExternalActNumberWriter : IExternalActNumberWriter
    {
        public Guid? AddCaseId { get; private set; }

        public Guid? AddActId { get; private set; }

        public ExternalNumberRequest? AddRequest { get; private set; }

        public ExternalActNumberOutcome AddOutcome { get; init; }

        public Guid? DeleteCaseId { get; private set; }

        public Guid? DeleteActId { get; private set; }

        public Guid? DeleteNumberId { get; private set; }

        public DeleteOutcome DeleteOutcome { get; init; }

        public async Task<ExternalActNumberOutcome> AddExternalActNumber(Guid caseId, Guid actId, ExternalNumberRequest request, CancellationToken token)
        {
            this.AddCaseId = caseId;
            this.AddActId = actId;
            this.AddRequest = request;

            return await Task.FromResult(this.AddOutcome);
        }

        public async Task<DeleteOutcome> DeleteExternalActNumber(Guid caseId, Guid actId, Guid numberId, CancellationToken token)
        {
            this.DeleteCaseId = caseId;
            this.DeleteActId = actId;
            this.DeleteNumberId = numberId;

            return await Task.FromResult(this.DeleteOutcome);
        }
    }
}
