using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class TenantActsControllerTests
{
    [Test]
    public async Task TheRequestReachesTheReaderUntouched()
    {
        var reader = new RecordingActReader();
        var controller = new TenantActsController();

        await controller.ListTenantActs(reader, new ActListRequest { Take = 5 }, CancellationToken.None);

        Assert.That(reader.TenantRequest?.Take, Is.EqualTo(5), "the controller decides nothing about the cap");
    }

    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingActReader { TenantItems = [Item("druhý"), Item("první")] };
        var controller = new TenantActsController();

        var response = await controller.ListTenantActs(reader, new ActListRequest(), CancellationToken.None);

        Assert.That(response.Items.Select(item => item.Title), Is.EqualTo(["druhý", "první"]), "the controller does not re-order what the reader gave it");
    }

    private static ActListItem Item(string title)
    {
        return new()
        {
            ActId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260821-001",
            ActNumber = "EC/20260821-001/20260822-001",
            Direction = ActDirection.Incoming,
            Title = title,
            Date = new DateOnly(2026, 8, 22),
            IssuedByName = "Úřad",
        };
    }

    private sealed class RecordingActReader : IActReader
    {
        public ActListRequest? TenantRequest { get; private set; }

        public IReadOnlyList<ActListItem> TenantItems { get; init; } = [];

        public Task<IReadOnlyList<ActListItem>> ListActs(Guid caseId, CancellationToken token)
        {
            return Task.FromResult<IReadOnlyList<ActListItem>>([]);
        }

        public Task<ActDetail?> GetActDetail(Guid caseId, Guid actId, CancellationToken token)
        {
            return Task.FromResult<ActDetail?>(null);
        }

        public Task<IReadOnlyList<ActListItem>> ListTenantActs(ActListRequest request, CancellationToken token)
        {
            this.TenantRequest = request;

            return Task.FromResult(this.TenantItems);
        }
    }
}
