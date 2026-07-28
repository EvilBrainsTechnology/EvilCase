using System.Net.Http.Json;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Api.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EvilBrains.EvilCase.Tests.Routing;

[TestFixture]
public class RefitRoutingApplicationModelProviderTests
{
    [Test]
    public async Task RouteAndMethodAreDerivedFromRefitInterfaceTest()
    {
        using var host = await StartHostAsync(typeof(EchoController));
        using var client = host.GetTestClient();

        var response = await client.PostAsJsonAsync("/echo", new EchoRequest { Message = "hi" });
        var echo = await response.Content.ReadFromJsonAsync<EchoResponse>();

        Assert.That(echo?.Message, Is.EqualTo("Echo: hi"));
    }

    [Test]
    public void RefitParameterAttributeIsRejectedTest()
    {
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => StartHostAsync(typeof(AliasController)));
        Assert.That(exception.Message, Does.Contain("AliasAs"));
    }

    [Test]
    public void RoutePlaceholderWithoutMatchingParameterIsRejectedTest()
    {
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => StartHostAsync(typeof(PlaceholderController)));
        Assert.That(exception.Message, Does.Contain("itemId"));
    }

    private static async Task<IHost> StartHostAsync(params Type[] controllers)
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services
                        .AddControllers()
                        .ConfigureApplicationPartManager(manager =>
                        {
                            manager.ApplicationParts.Clear();
                            manager.ApplicationParts.Add(new TypesApplicationPart(controllers));
                        });

                    services.TryAddEnumerable(
                        ServiceDescriptor.Transient<IApplicationModelProvider, RefitRoutingApplicationModelProvider>());
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }));

        return await builder.StartAsync();
    }
}
