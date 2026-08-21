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
    public async Task AMissingCaseIsNotFound()
    {
        var controller = new CasesController();

        var result = await controller.GetCase(new RecordingCaseReader(), Guid.NewGuid(), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>(), "an id the tenant cannot see is absent, never forbidden");
    }

    [Test]
    public async Task TheDetailIsReturnedAsTheReaderGaveIt()
    {
        var id = Guid.CreateVersion7();
        var detail = Detail(id, "EC/20260821-001", "Spis");
        var reader = new RecordingCaseReader { Case = detail };
        var controller = new CasesController();

        var result = await controller.GetCase(reader, id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That((result.Result as OkObjectResult)?.Value, Is.SameAs(detail));
            Assert.That(reader.DetailId, Is.EqualTo(id), "the reader sees the id from the route");
        }
    }

    [Test]
    public async Task TheUpdateReachesTheWriterUntouched()
    {
        var id = Guid.CreateVersion7();
        var writer = new RecordingCaseWriter();
        var controller = new CasesController();
        var request = new UpdateCaseRequest
        {
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Description = "Popis",
            Status = CaseStatus.WaitingOnAuthority,
        };

        await controller.UpdateCase(writer, id, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.Id, Is.EqualTo(id));
            Assert.That(writer.UpdateRequest?.CaseNumber, Is.EqualTo(request.CaseNumber));
            Assert.That(writer.UpdateRequest?.Date, Is.EqualTo(request.Date));
            Assert.That(writer.UpdateRequest?.Title, Is.EqualTo(request.Title));
            Assert.That(writer.UpdateRequest?.Description, Is.EqualTo(request.Description));
            Assert.That(writer.UpdateRequest?.Status, Is.EqualTo(request.Status));
        }
    }

    [Test]
    public async Task ASavedCaseAnswersWithNoContent()
    {
        var writer = new RecordingCaseWriter { Result = CaseUpdateStatus.Updated };
        var controller = new CasesController();

        var result = await controller.UpdateCase(writer, Guid.CreateVersion7(), UpdateRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task AnUpdateOfAMissingCaseIsNotFound()
    {
        var writer = new RecordingCaseWriter { Result = CaseUpdateStatus.NotFound };
        var controller = new CasesController();

        var result = await controller.UpdateCase(writer, Guid.CreateVersion7(), UpdateRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task AMalformedNumberIsAFieldError()
    {
        var writer = new RecordingCaseWriter { Result = CaseUpdateStatus.InvalidNumber };
        var controller = new CasesController();

        var result = await controller.UpdateCase(writer, Guid.CreateVersion7(), UpdateRequest(), CancellationToken.None);

        var problem = (result as ObjectResult)?.Value as ValidationProblemDetails;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem, Is.Not.Null, "a bad case number is reported on its field");
            Assert.That(problem?.Errors, Does.ContainKey(nameof(UpdateCaseRequest.CaseNumber)));
        }
    }

    [Test]
    public async Task ATakenNumberIsAConflict()
    {
        var writer = new RecordingCaseWriter { Result = CaseUpdateStatus.NumberTaken };
        var controller = new CasesController();

        var result = await controller.UpdateCase(writer, Guid.CreateVersion7(), UpdateRequest(), CancellationToken.None);

        var problem = result as ObjectResult;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem?.StatusCode, Is.EqualTo(409), "a taken case number is a state conflict, not a validation error");
            Assert.That(problem?.Value, Is.InstanceOf<ProblemDetails>());
        }
    }

    private static UpdateCaseRequest UpdateRequest()
    {
        return new()
        {
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };
    }

    private static CaseDetail Detail(Guid id, string caseNumber, string title)
    {
        return new()
        {
            Id = id,
            CaseNumber = caseNumber,
            Title = title,
            Date = new DateOnly(2026, 8, 21),
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

        public CaseDetail? Case { get; init; }

        public Guid DetailId { get; private set; }

        public Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default)
        {
            this.Request = request;

            return Task.FromResult(this.Items);
        }

        public Task<CaseDetail?> Detail(Guid id, CancellationToken cancellationToken = default)
        {
            this.DetailId = id;

            return Task.FromResult(this.Case);
        }
    }

    private sealed class RecordingCaseWriter : ICaseWriter
    {
        public CreateCaseRequest? Request { get; private set; }

        public CaseListItem Created { get; init; } = Item("EC/20260821-001", "Spis");

        public Guid Id { get; private set; }

        public UpdateCaseRequest? UpdateRequest { get; private set; }

        public CaseUpdateStatus Result { get; init; } = CaseUpdateStatus.Updated;

        public Task<CaseListItem> Create(CreateCaseRequest request, CancellationToken cancellationToken = default)
        {
            this.Request = request;

            return Task.FromResult(this.Created);
        }

        public Task<CaseUpdateStatus> Update(Guid id, UpdateCaseRequest request, CancellationToken cancellationToken = default)
        {
            this.Id = id;
            this.UpdateRequest = request;

            return Task.FromResult(this.Result);
        }
    }
}
