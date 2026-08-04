using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CasesControllerTests
{
    [Test]
    public async Task TheRequestReachesTheReaderUntouched()
    {
        var reader = new RecordingCaseReader();
        var controller = new CasesController(reader);
        var request = new CaseListRequest { Search = "odvolání", Status = CaseStatusFilter.WaitingOnAuthority };

        _ = await controller.ListCases(request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reader.Request?.Search, Is.EqualTo("odvolání"));
            Assert.That(reader.Request?.Status, Is.EqualTo(CaseStatusFilter.WaitingOnAuthority));
        }
    }

    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingCaseReader { Items = [Item(2, "druhý"), Item(1, "první")] };
        var controller = new CasesController(reader);

        var response = await controller.ListCases(new CaseListRequest(), CancellationToken.None);

        Assert.That(response.Items.Select(item => item.Title), Is.EqualTo(["druhý", "první"]));
    }

    private static CaseListItem Item(long id, string title) => new()
    {
        Id = id,
        Title = title,
        Status = CaseStatus.Active,
        Tags = [],
        Created = DateTime.UtcNow,
        SubCaseCount = 0,
    };

    private sealed class RecordingCaseReader : ICaseReader
    {
        public CaseListRequest? Request { get; private set; }

        public IReadOnlyList<CaseListItem> Items { get; init; } = [];

        public Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default)
        {
            this.Request = request;

            return Task.FromResult(this.Items);
        }
    }
}
