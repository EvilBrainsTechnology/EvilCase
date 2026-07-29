using System.Collections.Immutable;
using EvilBrains.ApiClient.Generator;
using Microsoft.CodeAnalysis;

namespace EvilBrains.Utils.Tests.ApiClient;

public class ActionParameterBindingAnalyzerTests
{
    [Test]
    public async Task CompliantActionHasNoDiagnosticsTest()
    {
        var diagnostics = await AnalyzeAsync("""
            [HttpGet("")]
            public string GetItems([FromQuery] string filter, [FromHeader(Name = "X-Tenant")] string? tenant, CancellationToken token) => filter;
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task ParameterWithoutBindingAttributeIsReportedTest()
    {
        var diagnostics = await AnalyzeAsync("""
            [HttpGet("")]
            public string GetItems(string filter) => filter;
            """);

        AssertIds(diagnostics, "EB1005");
    }

    [Test]
    public async Task ParameterWithMultipleBindingAttributesIsReportedTest()
    {
        var diagnostics = await AnalyzeAsync("""
            [HttpGet("")]
            public string GetItems([FromQuery] [FromHeader] string filter) => filter;
            """);

        AssertIds(diagnostics, "EB1005");
    }

    [Test]
    public async Task CancellationTokenWithBindingAttributeIsReportedTest()
    {
        var diagnostics = await AnalyzeAsync("""
            [HttpGet("")]
            public string GetItems([FromQuery] CancellationToken token) => "";
            """);

        AssertIds(diagnostics, "EB1005");
    }

    private static void AssertIds(in ImmutableArray<Diagnostic> diagnostics, params string[] expected) =>
        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(expected));

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string action) =>
        AnalyzerTestHost.AnalyzeAsync(new ActionParameterBindingAnalyzer(), Fixture(action));

    private static string Fixture(string action) => $$"""
        using System.Threading;
        using Microsoft.AspNetCore.Mvc;

        namespace FakeApi;

        [ApiController]
        [Route("items")]
        public class ItemsController : ControllerBase
        {
        {{action}}
        }
        """;
}
