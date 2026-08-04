using System.Net;
using System.Net.Http.Json;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Cases;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The detail through the real host: routing, binding and the nested tree surviving the wire. The
/// reader and the writer are stood in for, so nothing here opens a connection.
/// </summary>
public class CaseDetailEndpointTests
{
    private static readonly CaseDetailResponse Detail = FakeCases.Detail(7, "Přestupek") with
    {
        Ancestors = [new CaseAncestor { Id = 1, CaseNumber = "EC-001", Title = "Kořen" }],
        SubCases =
        [
            new CaseTreeNode
            {
                Id = 8,
                CaseNumber = "EC-008",
                Title = "Podspis",
                Status = CaseStatus.WaitingOnAuthority,
                Children =
                [
                    new CaseTreeNode { Id = 9, CaseNumber = "EC-009", Title = "Vnuk", Status = CaseStatus.Closed, Children = [] },
                ],
            },
        ],
        Comments = [FakeCases.Comment(3, "první zápis")],
    };

    private readonly RecordingCaseCommentWriter writer = new() { Comment = FakeCases.Comment(4, "nový zápis") };

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost(configureServices: services =>
        {
            services.AddSingleton<ICaseReader>(new RecordingCaseReader { Detail = Detail });
            services.AddSingleton<ICaseCommentWriter>(this.writer);
        });

        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [Test]
    public async Task TheDetailCarriesTheWholeSubTreeAndTheThread()
    {
        using var response = await this.Send(HttpMethod.Get, "/api/cases/7");

        var body = await response.Content.ReadFromJsonAsync<CaseDetailResponse>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body?.Title, Is.EqualTo("Přestupek"));
            Assert.That(body?.Ancestors.Select(ancestor => ancestor.Id), Is.EqualTo(new long[] { 1 }));
            Assert.That(body?.SubCases[0].Children[0].Id, Is.EqualTo(9), "the tree nests to any depth on the wire");
            Assert.That(body?.Comments[0].Body, Is.EqualTo("první zápis"));
        }
    }

    [Test]
    public async Task AnUnknownCaseIsNotFound()
    {
        using var response = await this.Send(HttpMethod.Get, "/api/cases/404");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// The list route is a literal segment next to the identifier one; one must not swallow the other.
    /// </summary>
    [Test]
    public async Task ListingStillRoutesToTheListRatherThanToADetail()
    {
        using var response = await this.Send(HttpMethod.Get, "/api/cases/list");

        var body = await response.Content.ReadFromJsonAsync<CaseListResponse>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body?.Items, Is.Empty);
        }
    }

    [Test]
    public async Task ACommentIsPostedToTheCaseInTheRoute()
    {
        using var response = await this.Send(HttpMethod.Post, "/api/cases/7/comments", new AddCaseCommentRequest { Body = "nový zápis" });

        var body = await response.Content.ReadFromJsonAsync<CaseComment>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(this.writer.CaseId, Is.EqualTo(7));
            Assert.That(body?.Body, Is.EqualTo("nový zápis"));
        }
    }

    [Test]
    public async Task AnEmptyCommentIsRefusedBeforeItReachesTheWriter()
    {
        using var response = await this.Send(HttpMethod.Post, "/api/cases/7/comments", new AddCaseCommentRequest { Body = "   " });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        }
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));

        request.Headers.Authorization = TestTokens.BearerFrom(this.host);

        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType());

        return await this.client.SendAsync(request);
    }
}
