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
        this.host = new EvilCaseHost(configureServices: static services => services.AddSingleton(ContactWriter()));
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
            Assert.That(problem?.Errors, Does.ContainKey("$.kind"), "a JSON conversion failure is reported under the JSON path of the field that carried the integer");
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

    private static IContactWriter ContactWriter()
    {
        var writer = Substitute.For<IContactWriter>();
        writer.CreateContact(Arg.Any<ContactEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(static call => new ContactListItem
            {
                ContactId = Guid.CreateVersion7(),
                Kind = call.Arg<ContactEditRequest>().Kind,
                Name = call.Arg<ContactEditRequest>().Name,
            });

        return writer;
    }
}
