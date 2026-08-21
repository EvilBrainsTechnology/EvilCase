using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CasesControllerTests
{
    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingCaseReader { Items = [Item("EC/20260821-002", "druhý"), Item("EC/20260821-001", "první")] };
        var controller = new CasesController(reader, new RecordingCaseWriter());

        var response = await controller.ListCases(new CaseListRequest(), CancellationToken.None);

        Assert.That(response.Items.Select(item => item.Title), Is.EqualTo(["druhý", "první"]), "the controller does not re-order what the reader gave it");
    }

    [Test]
    public async Task TheSearchTermReachesTheReaderUntouched()
    {
        var reader = new RecordingCaseReader();
        var controller = new CasesController(reader, new RecordingCaseWriter());

        await controller.ListCases(new CaseListRequest { Search = "odvolání", Status = CaseStatusFilter.WaitingOnAuthority }, CancellationToken.None);

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
        var controller = new CasesController(new RecordingCaseReader(), writer);
        var request = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek", Description = "Popis" };

        await controller.CreateCase(request, CancellationToken.None);

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
        var controller = new CasesController(new RecordingCaseReader(), writer);

        var response = await controller.CreateCase(
            new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Nový spis" },
            CancellationToken.None);

        Assert.That(response, Is.SameAs(created));
    }

    private static CaseListItem Item(string caseNumber, string title) => new()
    {
        Id = Guid.CreateVersion7(),
        CaseNumber = caseNumber,
        Title = title,
        Date = new DateOnly(2026, 8, 21),
        Status = CaseStatus.Active,
    };

    private sealed class RecordingCaseReader : ICaseReader
    {
        public IReadOnlyList<CaseListItem> Items { get; init; } = [];

        public CaseListRequest? Request { get; private set; }

        public Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default)
        {
            this.Request = request;

            return Task.FromResult(this.Items);
        }
    }

    private sealed class RecordingCaseWriter : ICaseWriter
    {
        public CreateCaseRequest? Request { get; private set; }

        public CaseListItem Created { get; init; } = Item("EC/20260821-001", "Spis");

        public Task<CaseListItem> Create(CreateCaseRequest request, CancellationToken cancellationToken = default)
        {
            this.Request = request;

            return Task.FromResult(this.Created);
        }
    }
}
