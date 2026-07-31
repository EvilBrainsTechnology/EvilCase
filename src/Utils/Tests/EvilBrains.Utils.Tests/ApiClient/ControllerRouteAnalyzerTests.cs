using System.Collections.Immutable;
using EvilBrains.ApiClient.Generator;
using Microsoft.CodeAnalysis;

namespace EvilBrains.Utils.Tests.ApiClient;

public class ControllerRouteAnalyzerTests
{
    [Test]
    public async Task CompliantControllerHasNoDiagnosticsTest()
    {
        var diagnostics = await AnalyzeAsync("""
            [ApiController]
            [Route("items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("item-list/{id}")]
                public string GetItems([FromRoute] string id) => id;
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task MissingControllerRouteIsReportedTest()
    {
        var diagnostics = await AnalyzeAsync("""
            [ApiController]
            public class ItemsController : ControllerBase
            {
                [HttpGet("")]
                public string GetItems() => "";
            }
            """);

        AssertIds(diagnostics, "EB1001");
    }

    [Test]
    public async Task MissingActionRouteIsReportedTest()
    {
        var diagnostics = await AnalyzeAsync("""
            [ApiController]
            [Route("items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet]
                public string GetItems() => "";
            }
            """);

        AssertIds(diagnostics, "EB1002");
    }

    [Test]
    public async Task LeadingSlashRouteIsReportedTest()
    {
        var diagnostics = await AnalyzeAsync("""
            [ApiController]
            [Route("/items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("")]
                public string GetItems() => "";
            }
            """);

        AssertIds(diagnostics, "EB1003");
    }

    [Test]
    public async Task RouteTokenIsReportedTest()
    {
        var diagnostics = await AnalyzeAsync("""
            [ApiController]
            [Route("[controller]")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("")]
                public string GetItems() => "";
            }
            """);

        AssertIds(diagnostics, "EB1003");
    }

    [Test]
    public async Task CatchAllRoutePlaceholderIsReportedTest()
    {
        var diagnostics = await AnalyzeAsync("""
            [ApiController]
            [Route("items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("{*path}")]
                public string GetItems() => "";
            }
            """);

        AssertIds(diagnostics, "EB1003");
    }

    [Test]
    public async Task NonKebabCaseRouteIsReportedTest()
    {
        var diagnostics = await AnalyzeAsync("""
            [ApiController]
            [Route("items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("Item_List")]
                public string GetItems() => "";
            }
            """);

        AssertIds(diagnostics, "EB1004");
    }

    [Test]
    public async Task ControllerWithoutApiControllerAttributeIsIgnoredTest()
    {
        var diagnostics = await AnalyzeAsync("""
            public class HelperController : ControllerBase
            {
                public string GetItems(string filter) => filter;
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    private static void AssertIds(in ImmutableArray<Diagnostic> diagnostics, params string[] expected) =>
        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(expected));

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string controller) =>
        AnalyzerTestHost.AnalyzeAsync(new ControllerRouteAnalyzer(), Fixture(controller));

    private static string Fixture(string controller) => $$"""
        using Microsoft.AspNetCore.Mvc;

        namespace FakeApi;

        {{controller}}
        """;
}
