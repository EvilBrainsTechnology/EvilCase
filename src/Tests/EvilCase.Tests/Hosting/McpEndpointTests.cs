using System.Net;
using System.Net.Http.Headers;
using System.Text;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Mcp;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Labels;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.ApiTokens;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The token store and the business services are fakes; the transport, the scheme, the boundary and the tool filters run as
/// in production.
/// </summary>
public class McpEndpointTests
{
    private const string ListToolsBody = """{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}""";

    private readonly FakeApiTokenStore store = new();

    private readonly List<Guid> tenantsSeen = [];

    private readonly User user = new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        Email = "agent@evilcase.test",
        PasswordHash = "not-verified-here",
        Role = UserRole.User,
    };

    private ILabelWriter labels = null!;

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.labels = Substitute.For<ILabelWriter>();
        this.labels
            .DeleteLabel(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(DeleteOutcome.Deleted);

        this.host = new EvilCaseHost(configureServices: services =>
        {
            services.AddSingleton<IApiTokenStore>(this.store);
            services.AddSingleton(this.labels);

            // Records the tenant the tool runs under; knows no case.
            services.AddScoped(provider =>
            {
                var userContext = provider.GetRequiredService<IUserContext>();
                var reader = Substitute.For<ICaseReader>();

                reader
                    .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
                    .Returns(_ =>
                    {
                        this.tenantsSeen.Add(userContext.TenantId);

                        return new CaseListResponse { Items = [], TotalCount = 0 };
                    });

                return reader;
            });
        });

        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [SetUp]
    public void ForgetCalls()
    {
        this.labels.ClearReceivedCalls();
        this.tenantsSeen.Clear();
    }

    [Test]
    public async Task ARequestWithoutATokenIsUnauthorized()
    {
        using var response = await this.Post(secret: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task TheBrowserSessionAloneIsUnauthorized()
    {
        using var response = await this.Post(secret: null, cookie: TestTokens.TokenFrom(this.host));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "the MCP endpoint accepts an API token only, never the cookie");
    }

    [Test]
    public async Task AJwtBearerIsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(McpRoute.Path, UriKind.Relative)) { Content = JsonRpc(ListToolsBody) };
        request.Headers.Authorization = TestTokens.BearerFrom(this.host);

        using var response = await this.client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "the MCP endpoint accepts an API token only, never an access token");
    }

    [Test]
    public async Task AForeignOriginIsForbidden()
    {
        var secret = this.store.Issue(this.user, ApiTokenScope.ReadWrite);

        using var response = await this.Post(secret, origin: "https://evil.example");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), "a browser page of another origin never reaches the MCP endpoint, token or not");
    }

    [Test]
    public async Task ARequestWithoutAnOriginPasses()
    {
        var secret = this.store.Issue(this.user, ApiTokenScope.ReadWrite);

        using var response = await this.Post(secret);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "a command-line client sends no Origin");
    }

    [Test]
    public async Task AReadOnlyTokenListsOnlyReadTools()
    {
        await using var mcp = await this.Connect(this.store.Issue(this.user, ApiTokenScope.ReadOnly));

        var tools = await mcp.ListToolsAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tools.Select(static tool => tool.Name), Does.Contain("list_cases").And.Contain("create_file_download_link"));
            Assert.That(tools.Select(static tool => tool.Name), Has.None.EqualTo("delete_label").And.None.EqualTo("create_file_upload_link"));
            Assert.That(tools.Select(static tool => tool.ProtocolTool.Annotations?.ReadOnlyHint), Has.All.True, "a read-only token sees only the tools annotated read-only");
        }
    }

    [Test]
    public async Task AReadOnlyTokenCannotCallAWriteTool()
    {
        await using var mcp = await this.Connect(this.store.Issue(this.user, ApiTokenScope.ReadOnly));

        var result = await mcp.CallToolAsync("delete_label", new Dictionary<string, object?>(StringComparer.Ordinal) { ["labelId"] = Guid.CreateVersion7() });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsError, Is.True, "a read-only token calling a hidden write tool gets a tool error");
            await this.labels.DidNotReceive().DeleteLabel(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task AReadWriteTokenCallsAWriteTool()
    {
        await using var mcp = await this.Connect(this.store.Issue(this.user, ApiTokenScope.ReadWrite));
        var labelId = Guid.CreateVersion7();

        var tools = await mcp.ListToolsAsync();
        var result = await mcp.CallToolAsync("delete_label", new Dictionary<string, object?>(StringComparer.Ordinal) { ["labelId"] = labelId });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tools.Select(static tool => tool.Name), Does.Contain("delete_label"));
            Assert.That(result.IsError, Is.Not.True);
            await this.labels.Received(1).DeleteLabel(labelId, Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task AToolRunsUnderTheTenantOfTheTokensUser()
    {
        await using var mcp = await this.Connect(this.store.Issue(this.user, ApiTokenScope.ReadOnly));

        var result = await mcp.CallToolAsync(
            "list_cases",
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["request"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["take"] = 10 } });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsError, Is.Not.True);
            Assert.That(this.tenantsSeen, Is.EqualTo([this.user.TenantId]), "a tool acts as the token's user, tenant included");
        }
    }

    [Test]
    public async Task AnInvalidArgumentIsAToolErrorWithTheValidationMessage()
    {
        await using var mcp = await this.Connect(this.store.Issue(this.user, ApiTokenScope.ReadOnly));

        var result = await mcp.CallToolAsync(
            "list_cases",
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["request"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["take"] = 101 } });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsError, Is.True);
            Assert.That(Text(result), Does.Contain("Take"), "a list takes the paging REST takes, Take at most 100");
            Assert.That(this.tenantsSeen, Is.Empty, "an invalid request never reaches the business service");
        }
    }

    [Test]
    public async Task AnUnknownEntityIsAToolErrorWithTheProblemTitle()
    {
        await using var mcp = await this.Connect(this.store.Issue(this.user, ApiTokenScope.ReadOnly));

        var result = await mcp.CallToolAsync("get_case", new Dictionary<string, object?>(StringComparer.Ordinal) { ["caseId"] = Guid.CreateVersion7() });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsError, Is.True);
            Assert.That(Text(result), Does.Contain(CaseProblems.NotFound), "a not-found answers with the Problem Details title REST answers with");
        }
    }

    [Test]
    public async Task EveryToolIsDescribedAndAnnotated()
    {
        await using var mcp = await this.Connect(this.store.Issue(this.user, ApiTokenScope.ReadWrite));

        var tools = await mcp.ListToolsAsync();

        using (Assert.EnterMultipleScope())
        {
            foreach (var tool in tools)
            {
                var annotations = tool.ProtocolTool.Annotations;
                var reads = tool.Name.StartsWith("list_", StringComparison.Ordinal)
                    || tool.Name.StartsWith("get_", StringComparison.Ordinal)
                    || tool.Name.StartsWith("read_", StringComparison.Ordinal)
                    || tool.Name.StartsWith("count_", StringComparison.Ordinal)
                    || tool.Name.StartsWith("verify_", StringComparison.Ordinal)
                    || string.Equals(tool.Name, "search", StringComparison.Ordinal)
                    || string.Equals(tool.Name, "create_file_download_link", StringComparison.Ordinal);

                Assert.That(tool.Description, Is.Not.Empty, $"{tool.Name}: every tool says what it does");
                Assert.That(annotations?.ReadOnlyHint == true, Is.EqualTo(reads), $"{tool.Name}: a read tool, and only a read tool, carries readOnlyHint");

                if (tool.Name.StartsWith("delete_", StringComparison.Ordinal))
                    Assert.That(annotations?.DestructiveHint, Is.True, $"{tool.Name}: a delete tool carries destructiveHint");
            }
        }
    }

    private static StringContent JsonRpc(string body)
    {
        return new StringContent(body, Encoding.UTF8, "application/json");
    }

    private static string Text(CallToolResult result)
    {
        return string.Concat(result.Content.OfType<TextContentBlock>().Select(static block => block.Text));
    }

    private async Task<HttpResponseMessage> Post(string? secret, string? cookie = null, string? origin = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(McpRoute.Path, UriKind.Relative)) { Content = JsonRpc(ListToolsBody) };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        if (secret is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);

        if (cookie is not null)
            request.Headers.Add("Cookie", $"{AccessCookie.Name}={cookie}");

        if (origin is not null)
            request.Headers.Add("Origin", origin);

        return await this.client.SendAsync(request);
    }

    private async Task<McpClient> Connect(string secret)
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(this.client.BaseAddress!, McpRoute.Path),
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string>(StringComparer.Ordinal) { ["Authorization"] = $"Bearer {secret}" },
            },
            this.client,
            loggerFactory: null,
            ownsHttpClient: false);

        return await McpClient.CreateAsync(transport);
    }
}
