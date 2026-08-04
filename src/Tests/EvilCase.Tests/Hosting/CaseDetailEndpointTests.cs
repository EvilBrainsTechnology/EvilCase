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
    /// <summary>
    /// Past every JSON serializer's default depth: MVC writes at 32 and the generated client reads at 64,
    /// which a nested sub-tree would have run into long before a case ran out of generations.
    /// </summary>
    private const int Generations = 200;

    private static readonly CaseDetailResponse Detail = FakeCases.Detail(7, "Přestupek") with
    {
        Ancestors = [new CaseAncestor { Id = 1, CaseNumber = "EC-001", Title = "Kořen" }],
        SubCases = Chain(7, Generations),
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
            Assert.That(body?.SubCases[1].ParentId, Is.EqualTo(body?.SubCases[0].Id), "a node carries the case it hangs under");
            Assert.That(body?.Comments[0].Body, Is.EqualTo("první zápis"));
        }
    }

    /// <summary>
    /// The sub-tree travels flat, so no serializer's depth stands between a case and its generations.
    /// </summary>
    [Test]
    public async Task ASubTreeDeeperThanAnySerializerAllowsSurvivesTheWire()
    {
        using var response = await this.Send(HttpMethod.Get, "/api/cases/7");

        var body = await response.Content.ReadFromJsonAsync<CaseDetailResponse>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "a depth the serializer refuses would answer 500");
            Assert.That(body?.SubCases, Has.Count.EqualTo(Generations));
            Assert.That(body?.SubCases[^1].Title, Is.EqualTo("Podspis 200"), "the deepest generation arrives whole");
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

    /// <summary>
    /// One sub-case per generation, each hanging under the one before it.
    /// </summary>
    private static IReadOnlyList<CaseTreeNode> Chain(long parentId, int generations) =>
        [.. Enumerable.Range(1, generations).Select(generation => new CaseTreeNode
        {
            Id = parentId + generation,
            ParentId = parentId + generation - 1,
            CaseNumber = string.Create(CultureInfo.InvariantCulture, $"EC-{generation:D3}"),
            Title = string.Create(CultureInfo.InvariantCulture, $"Podspis {generation}"),
            Status = CaseStatus.WaitingOnAuthority,
        }),
        ];

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));

        request.Headers.Authorization = TestTokens.BearerFrom(this.host);

        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType());

        return await this.client.SendAsync(request);
    }
}
