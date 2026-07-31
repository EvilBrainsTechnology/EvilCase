using Microsoft.CodeAnalysis;

namespace EvilBrains.Utils.Tests.ApiClient;

public class ApiClientGeneratorTests
{
    private const string DefaultContract = """
        namespace FakeApi.Contract;

        public record ItemRequest
        {
            public required string Name { get; init; }

            public int? Count { get; init; }
        }

        public record ItemResponse
        {
            public required string Name { get; init; }
        }
        """;

    private const string ValidController = """
        using EvilBrains.ApiClient;
        using FakeApi.Contract;
        using Microsoft.AspNetCore.Mvc;

        namespace FakeApi.Controllers;

        [ApiController]
        [GenerateApiClient]
        [Route("api/items")]
        public class ItemsController : ControllerBase
        {
            [HttpGet("{id}")]
            public Task<ItemResponse> GetItem([FromRoute] Guid id, [FromQuery] string? filter = null, CancellationToken token = default) => throw null!;

            [HttpGet("search")]
            public Task<ItemResponse> Search([FromQuery] ItemRequest request, [FromHeader(Name = "X-Tenant")] string? tenant = null) => throw null!;

            [HttpPost("")]
            public Task<ItemResponse> Create([FromBody] ItemRequest request, [FromServices] object service, CancellationToken token = default) => throw null!;

            [HttpDelete("{id}")]
            public Task Delete([FromRoute] Guid id) => throw null!;
        }
        """;

    private const string ReturnShapesController = """
        using EvilBrains.ApiClient;
        using FakeApi.Contract;
        using Microsoft.AspNetCore.Mvc;

        namespace FakeApi.Controllers;

        [ApiController]
        [GenerateApiClient]
        [Route("api/shapes")]
        public class ShapesController : ControllerBase
        {
            [HttpPost("create")]
            public Task<ActionResult> Create([FromBody] ItemRequest request) => throw null!;

            [HttpGet("find")]
            public Task<ActionResult<ItemResponse>> Find([FromQuery] string name) => throw null!;

            [HttpGet("info")]
            public ItemResponse Info() => throw null!;

            [HttpGet("legacy")]
            public IActionResult Legacy() => throw null!;

            [HttpDelete("purge")]
            public void Purge() => throw null!;

            [HttpGet("modern")]
            public ValueTask<ItemResponse> Modern() => throw null!;

            [HttpDelete("drop")]
            public ValueTask Drop() => throw null!;
        }
        """;

    private const string DuplicateControllers = """
        using EvilBrains.ApiClient;
        using Microsoft.AspNetCore.Mvc;

        namespace FakeApi.Controllers
        {
            [ApiController]
            [GenerateApiClient]
            [Route("api/items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("")]
                public Task<string> GetItems() => throw null!;
            }
        }

        namespace FakeApi.Controllers.Nested
        {
            [ApiController]
            [GenerateApiClient]
            [Route("api/nested-items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("")]
                public Task<string> GetItems() => throw null!;
            }
        }
        """;

    private const string TwoControllers = """
        using EvilBrains.ApiClient;
        using Microsoft.AspNetCore.Mvc;

        namespace FakeApi.Controllers;

        [ApiController]
        [GenerateApiClient]
        [Route("api/items")]
        public class ItemsController : ControllerBase
        {
            [HttpGet("")]
            public Task<string> GetItems() => throw null!;
        }

        [ApiController]
        [GenerateApiClient]
        [Route("api/logs")]
        public class LogsController : ControllerBase
        {
            [HttpPost("client")]
            public void Write() => throw null!;
        }
        """;

    private const string RegistrationConsumer = """
        using FakeApi.Client;
        using Microsoft.Extensions.DependencyInjection;

        namespace FakeApi.Consumer;

        internal sealed class TestHandler : DelegatingHandler;

        internal static class Registration
        {
            public static void Add(IServiceCollection services)
            {
                services.AddGeneratedApiClients(client => client.BaseAddress = new Uri("https://localhost"));

                services.AddGeneratedApiClients(
                    client => client.BaseAddress = new Uri("https://localhost"),
                    client => client.AddHttpMessageHandler<TestHandler>());
            }
        }
        """;

    [Test]
    public void RegistrationAcceptsClientConfigurationTest()
    {
        var (diagnostics, output) = GeneratorTestHost.Run(TwoControllers, RegistrationConsumer);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(diagnostics, Is.Empty);
            Assert.That(output.GetDiagnostics().Where(x => x.Severity >= DiagnosticSeverity.Warning), Is.Empty, "generated code must be warning-clean");
            Assert.That(output.GetTypeByMetadataName("FakeApi.Consumer.Registration"), Is.Not.Null);
        }
    }

    [Test]
    public void ValidControllerGeneratesCompilableClientTest()
    {
        var (diagnostics, output) = GeneratorTestHost.Run(ValidController, DefaultContract);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(diagnostics, Is.Empty);
            Assert.That(output.GetDiagnostics().Where(x => x.Severity >= DiagnosticSeverity.Warning), Is.Empty, "generated code must be warning-clean");
            Assert.That(output.GetTypeByMetadataName("FakeApi.Client.IItemsClient"), Is.Not.Null);
            Assert.That(output.GetTypeByMetadataName("FakeApi.Client.ItemsClient"), Is.Not.Null);
            Assert.That(output.GetTypeByMetadataName("FakeApi.Client.ApiClientRegistrations"), Is.Not.Null);
        }
    }

    [Test]
    public void FromServicesParameterIsOmittedFromClientTest()
    {
        var (_, output) = GeneratorTestHost.Run(ValidController, DefaultContract);

        var client = output.GetTypeByMetadataName("FakeApi.Client.IItemsClient")!;
        var create = (IMethodSymbol)client.GetMembers("Create").Single();
        string[] expected = ["request", "token"];

        Assert.That(create.Parameters.Select(x => x.Name), Is.EqualTo(expected));
    }

    [Test]
    public void EnumDefaultValueIsEmittedFromSymbolTest()
    {
        const string contract = """
            namespace FakeApi.Contract;

            public enum SortOrder
            {
                Ascending = 0,

                Descending = 1,
            }

            public record ItemResponse
            {
                public required string Name { get; init; }
            }
            """;

        var (diagnostics, output) = GeneratorTestHost.Run(
            """
            using EvilBrains.ApiClient;
            using FakeApi.Contract;
            using Microsoft.AspNetCore.Mvc;

            namespace FakeApi.Controllers;

            [ApiController]
            [GenerateApiClient]
            [Route("api/items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("")]
                public Task<ItemResponse> GetItems([FromQuery] SortOrder order = SortOrder.Descending) => throw null!;
            }
            """,
            contract);

        var client = output.GetTypeByMetadataName("FakeApi.Client.IItemsClient")!;
        var order = ((IMethodSymbol)client.GetMembers("GetItems").Single()).Parameters.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(diagnostics, Is.Empty);
            Assert.That(output.GetDiagnostics().Where(x => x.Severity >= DiagnosticSeverity.Warning), Is.Empty, "generated code must be warning-clean");
            Assert.That(order.ExplicitDefaultValue, Is.EqualTo(1), "the enum default must survive the round-trip through the generator");
        }
    }

    [Test]
    public void ConstantAndNamedRouteTemplatesAreSupportedTest()
    {
        var (diagnostics, output) = GeneratorTestHost.Run(
            """
            using EvilBrains.ApiClient;
            using FakeApi.Contract;
            using Microsoft.AspNetCore.Mvc;

            namespace FakeApi.Controllers;

            [ApiController]
            [GenerateApiClient]
            [Route("api/items")]
            public class ItemsController : ControllerBase
            {
                private const string SearchTemplate = "search";

                [HttpGet(SearchTemplate)]
                public Task<ItemResponse> Search() => throw null!;

                [HttpHead(template: "ping")]
                public Task Ping() => throw null!;
            }
            """,
            DefaultContract);

        var client = output.GetTypeByMetadataName("FakeApi.Client.IItemsClient");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(diagnostics, Is.Empty);
            Assert.That(client!.GetMembers("Search"), Is.Not.Empty, "a constant template must resolve");
            Assert.That(client.GetMembers("Ping"), Is.Not.Empty, "a named template argument and HttpHead must resolve");
        }
    }

    [Test]
    public void UnmarkedControllerIsIgnoredTest()
    {
        var (diagnostics, output) = GeneratorTestHost.Run("""
            using Microsoft.AspNetCore.Mvc;

            namespace FakeApi.Controllers;

            [ApiController]
            [Route("api/items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("")]
                public IActionResult GetItems() => throw null!;
            }
            """);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(diagnostics, Is.Empty);
            Assert.That(output.GetTypeByMetadataName("FakeApi.Client.IItemsClient"), Is.Null);
        }
    }

    [Test]
    public void DiagnosticCarriesControllerFileLocationTest()
    {
        var (diagnostics, _) = GeneratorTestHost.Run("""
            using EvilBrains.ApiClient;
            using Microsoft.AspNetCore.Mvc;

            namespace FakeApi.Controllers;

            [ApiController]
            [GenerateApiClient]
            public class ItemsController : ControllerBase
            {
            }
            """);

        var diagnostic = diagnostics.Single(x => string.Equals(x.Id, "EB1001", StringComparison.Ordinal));
        var lineSpan = diagnostic.Location.GetLineSpan();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lineSpan.Path, Is.EqualTo(GeneratorTestHost.ControllerPath));
            Assert.That(lineSpan.StartLinePosition.Line, Is.EqualTo(7), "diagnostic should point at the class identifier line");
        }
    }

    [Test]
    public void MissingControllerRouteIsReportedTest() =>
        AssertDiagnosticInController("EB1001", "", route: null);

    [Test]
    public void MissingActionRouteIsReportedTest() =>
        AssertDiagnostic(
            "EB1002",
            """
            [HttpGet]
            public Task<ItemResponse> GetItems() => throw null!;
            """);

    [Test]
    public void MultipleHttpMethodAttributesAreReportedTest() =>
        AssertDiagnostic(
            "EB1002",
            """
            [HttpGet("")]
            [HttpPost("")]
            public Task<ItemResponse> GetItems() => throw null!;
            """);

    [Test]
    public void LeadingSlashRouteIsReportedTest() =>
        AssertDiagnostic(
            "EB1003",
            """
            [HttpGet("/items")]
            public Task<ItemResponse> GetItems() => throw null!;
            """);

    [Test]
    public void RouteTokenIsReportedTest() =>
        AssertDiagnostic(
            "EB1003",
            """
            [HttpGet("[action]")]
            public Task<ItemResponse> GetItems() => throw null!;
            """);

    [Test]
    public void CatchAllRoutePlaceholderIsReportedTest() =>
        AssertDiagnostic(
            "EB1003",
            """
            [HttpGet("{*path}")]
            public Task<ItemResponse> GetItems() => throw null!;
            """);

    [Test]
    public void NonKebabCaseRouteIsReportedTest() =>
        AssertDiagnostic(
            "EB1004",
            """
            [HttpGet("items_list")]
            public Task<ItemResponse> GetItems() => throw null!;
            """);

    [Test]
    public void ControllerRouteWithoutApiPrefixIsReportedTest() =>
        AssertDiagnosticInController(
            "EB1006",
            """
            [HttpGet("")]
            public Task<ItemResponse> GetItems() => throw null!;
            """,
            "items");

    [Test]
    public void MissingBindingAttributeIsReportedTest() =>
        AssertDiagnostic(
            "EB1005",
            """
            [HttpGet("")]
            public Task<ItemResponse> GetItems(string filter) => throw null!;
            """);

    [Test]
    public void ReturnTypeWrappersAreUnwrappedTest()
    {
        var (diagnostics, output) = GeneratorTestHost.Run(ReturnShapesController, DefaultContract);

        const string task = "System.Threading.Tasks.Task";
        const string taskOfResponse = "System.Threading.Tasks.Task<FakeApi.Contract.ItemResponse>";
        var client = output.GetTypeByMetadataName("FakeApi.Client.IShapesClient")!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(diagnostics, Is.Empty);
            Assert.That(output.GetDiagnostics().Where(x => x.Severity >= DiagnosticSeverity.Warning), Is.Empty, "generated code must be warning-clean");
            Assert.That(ReturnTypeOf(client, "Create"), Is.EqualTo(task), "Task<ActionResult> carries no result");
            Assert.That(ReturnTypeOf(client, "Find"), Is.EqualTo(taskOfResponse), "Task<ActionResult<T>> unwraps to T");
            Assert.That(ReturnTypeOf(client, "Info"), Is.EqualTo(taskOfResponse), "a synchronous action becomes asynchronous");
            Assert.That(ReturnTypeOf(client, "Legacy"), Is.EqualTo(task), "IActionResult carries no result");
            Assert.That(ReturnTypeOf(client, "Purge"), Is.EqualTo(task), "void carries no result");
            Assert.That(ReturnTypeOf(client, "Modern"), Is.EqualTo(taskOfResponse), "ValueTask<T> unwraps to T");
            Assert.That(ReturnTypeOf(client, "Drop"), Is.EqualTo(task), "ValueTask carries no result");
        }
    }

    [Test]
    public void UnresolvableReturnTypeIsReportedTest() =>
        AssertDiagnostic(
            "EB1014",
            """
            [HttpGet("")]
            public Task<ActionResult<ServerOnlyResponse>> GetItems() => throw null!;
            """);

    [Test]
    public void UnmatchedRoutePlaceholderIsReportedTest() =>
        AssertDiagnostic(
            "EB1010",
            """
            [HttpGet("{itemId}")]
            public Task<ItemResponse> GetItem() => throw null!;
            """);

    [Test]
    public void RouteParameterWithoutPlaceholderIsReportedTest() =>
        AssertDiagnostic(
            "EB1010",
            """
            [HttpGet("")]
            public Task<ItemResponse> GetItem([FromRoute] Guid id) => throw null!;
            """);

    [Test]
    public void MultipleBodyParametersAreReportedTest() =>
        AssertDiagnostic(
            "EB1011",
            """
            [HttpPost("")]
            public Task Create([FromBody] ItemRequest first, [FromBody] ItemRequest second) => throw null!;
            """);

    [Test]
    public void FromFormParameterIsReportedTest() =>
        AssertDiagnostic(
            "EB1012",
            """
            [HttpPost("")]
            public Task Create([FromForm] string name) => throw null!;
            """);

    [Test]
    public void NullableRouteParameterIsReportedTest() =>
        AssertDiagnostic(
            "EB1013",
            """
            [HttpGet("{id}")]
            public Task<ItemResponse> GetItem([FromRoute] int? id) => throw null!;
            """);

    [Test]
    public void TypeNotVisibleToClientIsReportedTest() =>
        AssertDiagnostic(
            "EB1014",
            """
            [HttpPost("")]
            public Task Create([FromBody] ServerOnlyRequest request) => throw null!;
            """);

    [Test]
    public void ComplexQueryPropertyIsReportedTest()
    {
        const string contract = """
            namespace FakeApi.Contract;

            public record ItemResponse
            {
                public required string Name { get; init; }
            }

            public record ComplexQuery
            {
                public ItemResponse? Inner { get; init; }
            }
            """;

        AssertDiagnostic(
            "EB1015",
            """
            [HttpGet("")]
            public Task<ItemResponse> GetItems([FromQuery] ComplexQuery query) => throw null!;
            """,
            contract);
    }

    [Test]
    public void ShadowedQueryPropertyIsEmittedOnceTest()
    {
        const string contract = """
            namespace FakeApi.Contract;

            public class BaseQuery
            {
                public string? Name { get; init; }
            }

            public sealed class ItemQuery : BaseQuery
            {
                public new string? Name { get; init; }
            }

            public record ItemResponse
            {
                public required string Name { get; init; }
            }
            """;

        var (diagnostics, output) = GeneratorTestHost.Run(
            """
            using EvilBrains.ApiClient;
            using FakeApi.Contract;
            using Microsoft.AspNetCore.Mvc;

            namespace FakeApi.Controllers;

            [ApiController]
            [GenerateApiClient]
            [Route("api/items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("")]
                public Task<ItemResponse> GetItems([FromQuery] ItemQuery query) => throw null!;
            }
            """,
            contract);

        var source = output.SyntaxTrees.Single(x => x.FilePath.EndsWith("ItemsClient.g.cs", StringComparison.Ordinal)).ToString();
        var pairs = source.Split(["(\"name\""], StringSplitOptions.None).Length - 1;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(diagnostics, Is.Empty);
            Assert.That(pairs, Is.EqualTo(1), "a shadowed property must produce a single query pair");
        }
    }

    [Test]
    public void DuplicateClientNameIsReportedTest()
    {
        var (diagnostics, _) = GeneratorTestHost.Run(DuplicateControllers);

        Assert.That(diagnostics.Select(x => x.Id), Does.Contain("EB1016"));
    }

    private static string ReturnTypeOf(INamedTypeSymbol client, string method) =>
        ((IMethodSymbol)client.GetMembers(method).Single()).ReturnType.ToDisplayString();

    private static void AssertDiagnostic(string id, string action, string? contract = null) =>
        AssertDiagnosticInController(id, action, "api/items", contract);

    private static void AssertDiagnosticInController(string id, string action, string? route, string? contract = null)
    {
        var routeAttribute = route is null ? "" : $"[Route(\"{route}\")]";
        var controller = $$"""
            using EvilBrains.ApiClient;
            using FakeApi.Contract;
            using Microsoft.AspNetCore.Mvc;

            namespace FakeApi.Controllers;

            [ApiController]
            [GenerateApiClient]
            {{routeAttribute}}
            public class ItemsController : ControllerBase
            {
            {{action}}
            }
            """;

        var (diagnostics, _) = GeneratorTestHost.Run(controller, contract ?? DefaultContract);

        Assert.That(diagnostics.Select(x => x.Id), Does.Contain(id));
    }
}
