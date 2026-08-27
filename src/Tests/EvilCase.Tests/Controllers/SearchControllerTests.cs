using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Search;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class SearchControllerTests
{
    [Test]
    public async Task TheQueryReachesTheReaderUntouched()
    {
        var reader = new RecordingSearchReader();
        var controller = new SearchController();

        await controller.Search(reader, new SearchRequest { Query = "odvolání" }, CancellationToken.None);

        Assert.That(reader.Request?.Query, Is.EqualTo("odvolání"), "the controller decides nothing about the term");
    }

    [Test]
    public async Task TheAnswerIsWhatTheReaderReturned()
    {
        var expected = new SearchResponse { Items = [Item()] };
        var reader = new RecordingSearchReader { Response = expected };
        var controller = new SearchController();

        var response = await controller.Search(reader, new SearchRequest { Query = "odvolání" }, CancellationToken.None);

        Assert.That(response, Is.SameAs(expected), "the controller hands the reader's answer through untouched");
    }

    private static SearchResultItem Item()
    {
        return new()
        {
            Kind = SearchResultKind.Case,
            CaseId = Guid.CreateVersion7(),
            Number = "EC/20260821-001",
            Title = "Přestupek",
            Date = new DateOnly(2026, 8, 21),
        };
    }

    private sealed class RecordingSearchReader : ISearchReader
    {
        public SearchRequest? Request { get; private set; }

        public SearchResponse Response { get; init; } = new() { Items = [] };

        public Task<SearchResponse> Search(SearchRequest request, CancellationToken token)
        {
            this.Request = request;

            return Task.FromResult(this.Response);
        }
    }
}
