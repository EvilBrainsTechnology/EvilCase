using System.Net;
using System.Net.Http.Json;
using System.Text;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// <c>JsonStringEnumConverter&lt;T&gt;</c> defaults to accepting an integer alongside the name. A JSON
/// body could then name an enum member no name ever named, which the query-string binder already
/// refuses — SDD-004's validation layer must refuse it too.
/// </summary>
public class StrictEnumBindingTests
{
    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost(configureServices: services => services.AddSingleton<IContactWriter>(new StubContactWriter()));
        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [Test]
    public async Task AnIntegerEnumValueInTheBodyIsRefused()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/contacts", UriKind.Relative))
        {
            Headers = { Authorization = TestTokens.BearerFrom(this.host) },
            Content = new StringContent("""{"name":"Nový kontakt","kind":999}""", Encoding.UTF8, "application/json"),
        };

        using var response = await this.client.SendAsync(request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "an integer enum value in a JSON body bypasses no validation");
            Assert.That(problem?.Errors, Does.ContainKey(nameof(ContactEditRequest.Kind)), "the refused value is reported on the field that carries it");
        }
    }

    [Test]
    public async Task TheEnumMemberNameStillBinds()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/contacts", UriKind.Relative))
        {
            Headers = { Authorization = TestTokens.BearerFrom(this.host) },
            Content = JsonContent.Create(new ContactEditRequest { Name = "Nový kontakt", Kind = ContactKind.Authority }),
        };

        using var response = await this.client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "the strict converter still accepts the member by its name");
    }

    private sealed class StubContactWriter : IContactWriter
    {
        public Task<ContactListItem> CreateContact(ContactEditRequest request, CancellationToken token)
        {
            return Task.FromResult(new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = request.Kind, Name = request.Name });
        }

        public Task<ContactUpdateOutcome> UpdateContact(Guid contactId, ContactEditRequest request, CancellationToken token)
        {
            throw new NotSupportedException();
        }

        public Task<ContactDeleteOutcome> DeleteContact(Guid contactId, CancellationToken token)
        {
            throw new NotSupportedException();
        }
    }
}
