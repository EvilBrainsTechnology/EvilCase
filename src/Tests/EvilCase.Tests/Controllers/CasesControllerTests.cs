using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CasesControllerTests
{
    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingCaseReader { Items = [Item("EC/20260821-002", "druhý"), Item("EC/20260821-001", "první")] };
        var controller = new CasesController();

        var response = await controller.ListCases(reader, new CaseListRequest(), CancellationToken.None);

        Assert.That(response.Items.Select(item => item.Title), Is.EqualTo(["druhý", "první"]), "the controller does not re-order what the reader gave it");
    }

    [Test]
    public async Task TheSearchTermReachesTheReaderUntouched()
    {
        var reader = new RecordingCaseReader();
        var controller = new CasesController();

        await controller.ListCases(reader, new CaseListRequest { Search = "odvolání", Status = CaseStatusFilter.WaitingOnAuthority }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reader.Request?.Search, Is.EqualTo("odvolání"), "the controller decides nothing about the term");
            Assert.That(reader.Request?.Status, Is.EqualTo(CaseStatusFilter.WaitingOnAuthority), "the controller hands the status through untouched");
        }
    }

    [Test]
    public async Task TheRequestReachesTheWriterUntouched()
    {
        var writer = new RecordingCaseWriter();
        var controller = new CasesController();
        var request = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek", Description = "Popis" };

        await controller.CreateCase(writer, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.Request?.Date, Is.EqualTo(request.Date));
            Assert.That(writer.Request?.Title, Is.EqualTo(request.Title));
            Assert.That(writer.Request?.Description, Is.EqualTo(request.Description));
        }
    }

    [Test]
    public async Task TheCreatedCaseIsWhatTheWriterReturned()
    {
        var created = Item("EC/20260821-001", "Nový spis");
        var writer = new RecordingCaseWriter { Created = created };
        var controller = new CasesController();

        var response = await controller.CreateCase(
            writer,
            new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Nový spis" },
            CancellationToken.None);

        Assert.That(response, Is.SameAs(created));
    }

    [Test]
    public async Task TheDetailIsAskedForTheIdInTheRoute()
    {
        var caseId = Guid.CreateVersion7();
        var reader = new RecordingCaseReader { DetailResult = Detail(caseId) };
        var controller = new CasesController();

        await controller.GetCase(reader, caseId, CancellationToken.None);

        Assert.That(reader.DetailId, Is.EqualTo(caseId));
    }

    [Test]
    public async Task AMissingCaseIsAProblemWithFourOhFour()
    {
        var controller = new CasesController();

        var result = await controller.GetCase(new RecordingCaseReader { DetailResult = null }, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result.Result, 404);
    }

    [Test]
    public async Task AnEditReachesTheWriterWithTheRouteIdAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var writer = new RecordingCaseWriter { UpdateOutcome = CaseUpdateOutcome.Updated };
        var controller = new CasesController();
        var request = Edit();

        await controller.EditCase(writer, caseId, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.UpdateId, Is.EqualTo(caseId));
            Assert.That(writer.UpdateRequest, Is.SameAs(request), "the controller decides nothing about the edit");
        }
    }

    [Test]
    public async Task AnEditThatSucceedsAnswersWithNoContent()
    {
        var writer = new RecordingCaseWriter { UpdateOutcome = CaseUpdateOutcome.Updated };
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task EditingAMissingCaseIsAProblemWithFourOhFour()
    {
        var writer = new RecordingCaseWriter { UpdateOutcome = CaseUpdateOutcome.NotFound };
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task ACaseNumberOutsideTheFormatIsAFieldErrorOnTheNumber()
    {
        var writer = new RecordingCaseWriter { UpdateOutcome = CaseUpdateOutcome.InvalidCaseNumber };
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

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
            Does.ContainKey(nameof(CaseEditRequest.CaseNumber)),
            "a hand-written number outside the format is reported on the field that carries it");
    }

    [Test]
    public async Task ACaseNumberAnotherCaseHoldsIsAConflict()
    {
        var writer = new RecordingCaseWriter { UpdateOutcome = CaseUpdateOutcome.CaseNumberTaken };
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Detail, Is.Not.Null, "a number another case holds is a conflict the user resolves");
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

    private static CaseDetail Detail(Guid caseId)
    {
        return new()
        {
            Id = caseId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };
    }

    private static CaseEditRequest Edit()
    {
        return new()
        {
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };
    }

    private static CaseListItem Item(string caseNumber, string title)
    {
        return new()
        {
            Id = Guid.CreateVersion7(),
            CaseNumber = caseNumber,
            Title = title,
            Date = new DateOnly(2026, 8, 21),
            Status = CaseStatus.Active,
        };
    }

    private sealed class RecordingCaseReader : ICaseReader
    {
        public IReadOnlyList<CaseListItem> Items { get; init; } = [];

        public CaseListRequest? Request { get; private set; }

        public Guid? DetailId { get; private set; }

        public CaseDetail? DetailResult { get; init; }

        public Task<IReadOnlyList<CaseListItem>> ListCases(CaseListRequest request, CancellationToken token)
        {
            this.Request = request;

            return Task.FromResult(this.Items);
        }

        public Task<CaseDetail?> GetCaseDetail(Guid caseId, CancellationToken token)
        {
            this.DetailId = caseId;

            return Task.FromResult(this.DetailResult);
        }
    }

    private sealed class RecordingCaseWriter : ICaseWriter
    {
        public CreateCaseRequest? Request { get; private set; }

        public CaseListItem Created { get; init; } = Item("EC/20260821-001", "Spis");

        public Guid? UpdateId { get; private set; }

        public CaseEditRequest? UpdateRequest { get; private set; }

        public CaseUpdateOutcome UpdateOutcome { get; init; }

        public Task<CaseListItem> CreateCase(CreateCaseRequest request, CancellationToken token)
        {
            this.Request = request;

            return Task.FromResult(this.Created);
        }

        public Task<CaseUpdateOutcome> UpdateCase(Guid caseId, CaseEditRequest request, CancellationToken token)
        {
            this.UpdateId = caseId;
            this.UpdateRequest = request;

            return Task.FromResult(this.UpdateOutcome);
        }
    }
}
