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
        [Route("items")]
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
        [Route("shapes")]
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
        }
        """;

    private const string DuplicateControllers = """
        using EvilBrains.ApiClient;
        using Microsoft.AspNetCore.Mvc;

        namespace FakeApi.Controllers
        {
            [ApiController]
            [GenerateApiClient]
            [Route("items")]
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
            [Route("nested_items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("")]
                public Task<string> GetItems() => throw null!;
            }
        }
        """;

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
    public void UnmarkedControllerIsIgnoredTest()
    {
        var (diagnostics, output) = GeneratorTestHost.Run("""
            using Microsoft.AspNetCore.Mvc;

            namespace FakeApi.Controllers;

            [ApiController]
            [Route("items")]
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
    public void NonSnakeCaseRouteIsReportedTest() =>
        AssertDiagnostic(
            "EB1004",
            """
            [HttpGet("items-list")]
            public Task<ItemResponse> GetItems() => throw null!;
            """);

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
    public void DuplicateClientNameIsReportedTest()
    {
        var (diagnostics, _) = GeneratorTestHost.Run(DuplicateControllers);

        Assert.That(diagnostics.Select(x => x.Id), Does.Contain("EB1016"));
    }

    private static string ReturnTypeOf(INamedTypeSymbol client, string method) =>
        ((IMethodSymbol)client.GetMembers(method).Single()).ReturnType.ToDisplayString();

    private static void AssertDiagnostic(string id, string action, string? contract = null) =>
        AssertDiagnosticInController(id, action, "items", contract);

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
