using System.Net;
using System.Text.Json;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The host runs as Testing, so this is the document outside Development.
/// </summary>
public class OpenApiDocumentTests
{
    [Test]
    public async Task TheDocumentIsAnonymousAndDeclaresTheBearerScheme()
    {
        await using var host = new EvilCaseHost();
        using var client = host.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var scheme = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "an integration reads the contract before it holds a token");
            Assert.That(scheme.GetProperty("type").GetString(), Is.EqualTo("http"));
            Assert.That(scheme.GetProperty("scheme").GetString(), Is.EqualTo("bearer"));
        }
    }
}
