using System.Net;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

public class ListRequestBindingTests
{
    private readonly List<CaseListRequest> bound = [];

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [SetUp]
    public void SetUp()
    {
        this.bound.Clear();

        var reader = Substitute.For<ICaseReader>();
        reader
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CaseListResponse { Items = [], TotalCount = 0 })
            .AndDoes(call => this.bound.Add(call.Arg<CaseListRequest>()));

        this.host = new EvilCaseHost(configureServices: services => services.AddSingleton(reader));
        this.client = this.host.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [Test]
    public async Task ARepeatedLabelParameterBindsAsEveryLabelTheFilterAsksFor()
    {
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();

        using var response = await this.Get($"/api/cases?take=20&labelIds={first}&labelIds={second}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(this.bound.Single().LabelIds, Is.EqualTo([first, second]), "a query parameter repeated on the wire reaches the filter as every value it carried");
        }
    }

    [Test]
    public async Task AListWithoutAPageSizeIsRefused()
    {
        using var response = await this.Get("/api/cases");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "the page size is required, so a list call omitting it never reaches the reader");
            Assert.That(this.bound, Is.Empty);
        }
    }

    [Test]
    public async Task APageSizeAboveTheHundredIsRefused()
    {
        using var response = await this.Get("/api/cases?take=101");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "the page limit holds at the wire, not only on the record");
    }

    private async Task<HttpResponseMessage> Get(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative)) { Headers = { Authorization = TestTokens.BearerFrom(this.host) } };

        return await this.client.SendAsync(request);
    }
}
