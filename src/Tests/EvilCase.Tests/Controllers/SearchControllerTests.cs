using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Search;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class SearchControllerTests
{
    [Test]
    public async Task TheRequestReachesTheReaderUntouchedAndItsAnswerComesBack()
    {
        var answer = new SearchResponse
        {
            Cases = Empty<CaseListItem>(),
            Acts = Empty<ActListItem>(),
            Contacts = Empty<ContactListItem>(),
            Files = Empty<FileSearchItem>(),
            Comments = Empty<CommentSearchItem>(),
        };
        var reader = Substitute.For<ISearchReader>();
        var request = new SearchRequest { Q = "poj" };
        reader.Search(request, Arg.Any<CancellationToken>()).Returns(answer);
        var controller = new SearchController();

        var response = await controller.Search(reader, request, CancellationToken.None);

        Assert.That(response, Is.SameAs(answer));
    }

    private static SearchGroup<TItem> Empty<TItem>()
    {
        return new SearchGroup<TItem> { Items = [], TotalCount = 0 };
    }
}
