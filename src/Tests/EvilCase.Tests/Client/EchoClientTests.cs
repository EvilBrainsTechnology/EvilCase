using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract;
using EvilBrains.EvilCase.Api.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EvilBrains.EvilCase.Tests.Client;

[TestFixture]
public class EchoClientTests
{
    [Test]
    public async Task EchoGetSendsDtoAsQueryParametersTest()
    {
        using var host = await StartHostAsync(withControllers: true);
        await using var services = BuildClientServices(host);
        var client = services.GetRequiredService<IEchoClient>();

        var response = await client.EchoGet(new EchoRequest { Message = "hi from query" });

        Assert.That(response.Message, Is.EqualTo("Echo: hi from query"));
    }

    [Test]
    public async Task EchoPostSendsDtoAsJsonBodyTest()
    {
        using var host = await StartHostAsync(withControllers: true);
        await using var services = BuildClientServices(host);
        var client = services.GetRequiredService<IEchoClient>();

        var response = await client.EchoPost(new EchoRequest { Message = "hi from body" });

        Assert.That(response.Message, Is.EqualTo("Echo: hi from body"));
    }

    [Test]
    public async Task MissingRouteThrowsApiExceptionTest()
    {
        using var host = await StartHostAsync(withControllers: false);
        await using var services = BuildClientServices(host);
        var client = services.GetRequiredService<IEchoClient>();

        var exception = Assert.ThrowsAsync<ApiException>(() => client.EchoPost(new EchoRequest { Message = "hi" }));
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private static async Task<IHost> StartHostAsync(bool withControllers)
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    var controllers = services.AddControllers();
                    if (withControllers)
                        controllers.AddApplicationPart(typeof(EchoController).Assembly);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }));

        return await builder.StartAsync();
    }

    private static ServiceProvider BuildClientServices(IHost host)
    {
        var handler = host.GetTestServer().CreateHandler();

        var services = new ServiceCollection();
        services.AddEvilCaseApiClient(new Uri("http://localhost"));
        services.AddHttpClient(nameof(IEchoClient)).ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider();
    }
}
