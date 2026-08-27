using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

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
    public async Task TheCountsAreWhatTheReaderRead()
    {
        var counts = new CaseStatusCounts { Active = 2, WaitingOnAuthority = 1, Closed = 3 };
        var reader = new RecordingCaseReader { Counts = counts };
        var controller = new CasesController();

        var response = await controller.CountCases(reader, CancellationToken.None);

        Assert.That(response, Is.EqualTo(counts), "the counts endpoint answers the counts the reader read, which is what feeds the dashboard tile");
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
        var request = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek", Description = "Popis", ParentCaseId = Guid.CreateVersion7() };

        await controller.CreateCase(writer, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.Request?.Date, Is.EqualTo(request.Date));
            Assert.That(writer.Request?.Title, Is.EqualTo(request.Title));
            Assert.That(writer.Request?.Description, Is.EqualTo(request.Description));
            Assert.That(writer.Request?.ParentCaseId, Is.EqualTo(request.ParentCaseId));
        }
    }

    [Test]
    public async Task TheCreatedCaseIsWhatTheWriterReturned()
    {
        var created = Item("EC/20260821-001", "Nový spis");
        var writer = new RecordingCaseWriter { CreateResult = new CaseCreateResult { Outcome = CaseCreateOutcome.Created, Case = created } };
        var controller = new CasesController();

        var response = await controller.CreateCase(
            writer,
            new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Nový spis" },
            CancellationToken.None);

        Assert.That((response.Result as CreatedAtActionResult)?.Value, Is.SameAs(created), "the created case travels back in the response body");
    }

    [Test]
    public async Task AFiledCaseIsAnsweredWithCreatedAtItsDetailRoute()
    {
        var created = Item("EC/20260821-001", "Nový spis");
        var writer = new RecordingCaseWriter { CreateResult = new CaseCreateResult { Outcome = CaseCreateOutcome.Created, Case = created } };
        var controller = new CasesController();

        var response = await controller.CreateCase(
            writer,
            new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Nový spis" },
            CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)response.Result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(201), "a create answers 201, not 200");
            Assert.That(result.ActionName, Is.EqualTo(nameof(CasesController.GetCase)), "the Location names the detail action of the case");
            Assert.That(result.RouteValues?["caseId"], Is.EqualTo(created.CaseId), "the Location carries the id of the case that was filed");
        }
    }

    [Test]
    public async Task FilingUnderAParentThatIsNoCaseIsAConflict()
    {
        var writer = new RecordingCaseWriter { CreateResult = new CaseCreateResult { Outcome = CaseCreateOutcome.InvalidParent } };
        var controller = new CasesController();

        var result = await controller.CreateCase(
            writer,
            new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Nový spis", ParentCaseId = Guid.CreateVersion7() },
            CancellationToken.None);

        var problem = AssertProblem(result.Result, 409);

        Assert.That(problem.Detail, Is.Not.Null, "a parent that is no case of the tenant is a conflict the user resolves");
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

    [Test]
    public async Task AParentThatWouldCloseALoopIsAConflict()
    {
        var writer = new RecordingCaseWriter { UpdateOutcome = CaseUpdateOutcome.InvalidParent };
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo("Invalid parent"), "the edit's two conflicts are told apart by the problem title");
    }

    [Test]
    public async Task DeletingACaseReachesTheWriterWithTheRouteId()
    {
        var caseId = Guid.CreateVersion7();
        var writer = new RecordingCaseWriter { DeleteOutcome = DeleteOutcome.Deleted };
        var controller = new CasesController();

        await controller.DeleteCase(writer, caseId, CancellationToken.None);

        Assert.That(writer.DeleteId, Is.EqualTo(caseId));
    }

    [Test]
    public async Task DeletingACaseAnswersWithNoContent()
    {
        var writer = new RecordingCaseWriter { DeleteOutcome = DeleteOutcome.Deleted };
        var controller = new CasesController();

        var result = await controller.DeleteCase(writer, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAMissingCaseIsAProblemWithFourOhFour()
    {
        var writer = new RecordingCaseWriter { DeleteOutcome = DeleteOutcome.NotFound };
        var controller = new CasesController();

        var result = await controller.DeleteCase(writer, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task ADeleteOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = new RecordingCaseWriter { DeleteOutcome = (DeleteOutcome)99 };
        var controller = new CasesController();

        await Assert.ThatAsync(
            () => controller.DeleteCase(writer, Guid.CreateVersion7(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    [Test]
    public async Task AddingAMarkReachesTheWriterWithTheRouteIdAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var writer = new RecordingExternalCaseNumberWriter { AddOutcome = ExternalCaseNumberOutcome.Added };
        var controller = new CasesController();
        var request = Mark();

        await controller.AddExternalCaseNumber(writer, caseId, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.AddCaseId, Is.EqualTo(caseId));
            Assert.That(writer.AddRequest, Is.SameAs(request), "the controller decides nothing about the mark");
        }
    }

    [Test]
    public async Task AddingAMarkThatSucceedsAnswersWithNoContent()
    {
        var writer = new RecordingExternalCaseNumberWriter { AddOutcome = ExternalCaseNumberOutcome.Added };
        var controller = new CasesController();

        var result = await controller.AddExternalCaseNumber(writer, Guid.CreateVersion7(), Mark(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task AddingAMarkToAMissingCaseIsAProblemWithFourOhFour()
    {
        var writer = new RecordingExternalCaseNumberWriter { AddOutcome = ExternalCaseNumberOutcome.CaseNotFound };
        var controller = new CasesController();

        var result = await controller.AddExternalCaseNumber(writer, Guid.CreateVersion7(), Mark(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task AMarkTheCaseAlreadyCarriesIsAConflict()
    {
        var writer = new RecordingExternalCaseNumberWriter { AddOutcome = ExternalCaseNumberOutcome.ValueTaken };
        var controller = new CasesController();

        var result = await controller.AddExternalCaseNumber(writer, Guid.CreateVersion7(), Mark(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo(ExternalNumberProblems.Taken), "the two conflicts of the add are told apart by the problem title");
    }

    [Test]
    public async Task AMarkNamingAnUnknownContactIsAConflict()
    {
        var writer = new RecordingExternalCaseNumberWriter { AddOutcome = ExternalCaseNumberOutcome.UnknownContact };
        var controller = new CasesController();

        var result = await controller.AddExternalCaseNumber(writer, Guid.CreateVersion7(), Mark(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo(ContactProblems.UnknownContact));
    }

    [Test]
    public async Task DeletingAMarkAnswersWithNoContent()
    {
        var writer = new RecordingExternalCaseNumberWriter { DeleteOutcome = DeleteOutcome.Deleted };
        var controller = new CasesController();

        var result = await controller.DeleteExternalCaseNumber(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAMarkReachesTheWriterWithBothRouteIds()
    {
        var caseId = Guid.CreateVersion7();
        var numberId = Guid.CreateVersion7();
        var writer = new RecordingExternalCaseNumberWriter { DeleteOutcome = DeleteOutcome.Deleted };
        var controller = new CasesController();

        await controller.DeleteExternalCaseNumber(writer, caseId, numberId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.DeleteCaseId, Is.EqualTo(caseId));
            Assert.That(writer.DeleteNumberId, Is.EqualTo(numberId));
        }
    }

    [Test]
    public async Task DeletingAMissingMarkIsAProblemWithFourOhFour()
    {
        var writer = new RecordingExternalCaseNumberWriter { DeleteOutcome = DeleteOutcome.NotFound };
        var controller = new CasesController();

        var result = await controller.DeleteExternalCaseNumber(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task AMarkDeleteOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = new RecordingExternalCaseNumberWriter { DeleteOutcome = (DeleteOutcome)99 };
        var controller = new CasesController();

        await Assert.ThatAsync(
            () => controller.DeleteExternalCaseNumber(writer, Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    private static ExternalNumberRequest Mark()
    {
        return new() { Value = "VV41/2025/08464", AssignedByContactId = Guid.CreateVersion7() };
    }

    private static CaseDetail Detail(Guid caseId)
    {
        return new()
        {
            CaseId = caseId,
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
            CaseId = Guid.CreateVersion7(),
            CaseNumber = caseNumber,
            Title = title,
            Date = new DateOnly(2026, 8, 21),
            Status = CaseStatus.Active,
            Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    private sealed class RecordingCaseReader : ICaseReader
    {
        public IReadOnlyList<CaseListItem> Items { get; init; } = [];

        public CaseListRequest? Request { get; private set; }

        public Guid? DetailId { get; private set; }

        public CaseDetail? DetailResult { get; init; }

        public CaseStatusCounts Counts { get; init; } = new() { Active = 0, WaitingOnAuthority = 0, Closed = 0 };

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

        public Task<CaseStatusCounts> CountCasesByStatus(CancellationToken token)
        {
            return Task.FromResult(this.Counts);
        }
    }

    private sealed class RecordingCaseWriter : ICaseWriter
    {
        public CreateCaseRequest? Request { get; private set; }

        public CaseCreateResult CreateResult { get; init; } = new() { Outcome = CaseCreateOutcome.Created, Case = Item("EC/20260821-001", "Spis") };

        public Guid? UpdateId { get; private set; }

        public CaseEditRequest? UpdateRequest { get; private set; }

        public CaseUpdateOutcome UpdateOutcome { get; init; }

        public Task<CaseCreateResult> CreateCase(CreateCaseRequest request, CancellationToken token)
        {
            this.Request = request;

            return Task.FromResult(this.CreateResult);
        }

        public Task<CaseUpdateOutcome> UpdateCase(Guid caseId, CaseEditRequest request, CancellationToken token)
        {
            this.UpdateId = caseId;
            this.UpdateRequest = request;

            return Task.FromResult(this.UpdateOutcome);
        }

        public Guid? DeleteId { get; private set; }

        public DeleteOutcome DeleteOutcome { get; init; }

        public Task<DeleteOutcome> DeleteCase(Guid caseId, CancellationToken token)
        {
            this.DeleteId = caseId;

            return Task.FromResult(this.DeleteOutcome);
        }
    }

    private sealed class RecordingExternalCaseNumberWriter : IExternalCaseNumberWriter
    {
        public Guid? AddCaseId { get; private set; }

        public ExternalNumberRequest? AddRequest { get; private set; }

        public ExternalCaseNumberOutcome AddOutcome { get; init; }

        public Guid? DeleteCaseId { get; private set; }

        public Guid? DeleteNumberId { get; private set; }

        public DeleteOutcome DeleteOutcome { get; init; }

        public Task<ExternalCaseNumberOutcome> AddExternalCaseNumber(Guid caseId, ExternalNumberRequest request, CancellationToken token)
        {
            this.AddCaseId = caseId;
            this.AddRequest = request;

            return Task.FromResult(this.AddOutcome);
        }

        public Task<DeleteOutcome> DeleteExternalCaseNumber(Guid caseId, Guid numberId, CancellationToken token)
        {
            this.DeleteCaseId = caseId;
            this.DeleteNumberId = numberId;

            return Task.FromResult(this.DeleteOutcome);
        }
    }
}
