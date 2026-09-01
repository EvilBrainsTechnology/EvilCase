using System.Net;
using System.Net.Http.Json;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// <c>CreatedAtAction</c> names an action and route values that only a route of the running host can
/// match, so the <c>Location</c> it produces is pinned here.
/// </summary>
public class CreateLocationTests
{
    private static readonly Guid FiledCaseId = Guid.CreateVersion7();

    private static readonly Guid FiledContactId = Guid.CreateVersion7();

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost(configureServices: static services =>
        {
            services.AddSingleton(CaseWriter());
            services.AddSingleton(ContactWriter());
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
    public async Task AFiledCaseCarriesTheLocationOfItsDetail()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/cases", UriKind.Relative))
        {
            Headers = { Authorization = TestTokens.BearerFrom(this.host) },
            Content = JsonContent.Create(new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Nový spis" }),
        };

        using var response = await this.client.SendAsync(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(
                response.Headers.Location?.ToString(),
                Is.EqualTo($"http://localhost/api/cases/{FiledCaseId}"),
                "the Location names the detail route of the case that was filed");
        }
    }

    [Test]
    public async Task AFiledContactCarriesTheLocationOfItsDetail()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/contacts", UriKind.Relative))
        {
            Headers = { Authorization = TestTokens.BearerFrom(this.host) },
            Content = JsonContent.Create(new ContactEditRequest { Name = "Nový kontakt", Kind = ContactKind.Authority }),
        };

        using var response = await this.client.SendAsync(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(
                response.Headers.Location?.ToString(),
                Is.EqualTo($"http://localhost/api/contacts/{FiledContactId}"),
                "the Location names the detail route of the contact that was filed");
        }
    }

    private static ICaseWriter CaseWriter()
    {
        var writer = Substitute.For<ICaseWriter>();
        writer.CreateCase(Arg.Any<CreateCaseRequest>(), Arg.Any<CancellationToken>())
            .Returns(static call => new CaseCreateResult
            {
                Outcome = CaseCreateOutcome.Created,
                Case = new CaseListItem
                {
                    CaseId = FiledCaseId,
                    CaseNumber = "EC/20260821-001",
                    Title = call.Arg<CreateCaseRequest>().Title,
                    Date = call.Arg<CreateCaseRequest>().Date,
                    Status = CaseStatus.Active,
                    Changed = DateTime.UtcNow,
                },
            });

        return writer;
    }

    private static IContactWriter ContactWriter()
    {
        var writer = Substitute.For<IContactWriter>();
        writer.CreateContact(Arg.Any<ContactEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(static call => new ContactListItem
            {
                ContactId = FiledContactId,
                Kind = call.Arg<ContactEditRequest>().Kind,
                Name = call.Arg<ContactEditRequest>().Name,
            });

        return writer;
    }
}
