using System.Collections.Immutable;
using EvilBrains.ApiClient.Generator;
using Microsoft.CodeAnalysis;

namespace EvilBrains.Utils.Tests.ApiClient;

public class ActionParameterOrderAnalyzerTests
{
    [Test]
    public async Task EveryKindInOrderHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze(
            """
            [HttpGet("{id}")]
            public string GetItem([FromServices] IThing thing, [FromRoute] Guid id, [FromQuery] string filter, [FromBody] string body, CancellationToken token) => filter;
            """);

        Assert.That(diagnostics, Is.Empty, "the canonical order is accepted");
    }

    [Test]
    public async Task ServicesAfterBodyIsReportedTest()
    {
        var diagnostics = await Analyze(
            """
            [HttpPost("")]
            public string Create([FromBody] string body, [FromServices] IThing thing) => body;
            """);

        AssertIds(diagnostics, "[FromServices] must precede [FromBody]");
    }

    [Test]
    public async Task RouteAfterQueryIsReportedTest()
    {
        var diagnostics = await Analyze(
            """
            [HttpGet("{id}")]
            public string GetItem([FromQuery] string filter, [FromRoute] Guid id) => filter;
            """);

        AssertIds(diagnostics, "[FromRoute] must precede [FromQuery]");
    }

    [Test]
    public async Task QueryAfterBodyIsReportedTest()
    {
        var diagnostics = await Analyze(
            """
            [HttpPost("")]
            public string Create([FromBody] string body, [FromQuery] string filter) => body;
            """);

        AssertIds(diagnostics, "[FromQuery] must precede [FromBody]");
    }

    [Test]
    public async Task TokenBeforeBodyIsReportedTest()
    {
        var diagnostics = await Analyze(
            """
            [HttpPost("")]
            public string Create(CancellationToken token, [FromBody] string body) => body;
            """);

        AssertIds(diagnostics, "the CancellationToken comes last");
    }

    [Test]
    public async Task SameRankInAnyOrderHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze(
            """
            [HttpGet("{id}/{other}")]
            public string GetItem([FromRoute] Guid other, [FromRoute] Guid id) => "";
            """);

        Assert.That(diagnostics, Is.Empty, "parameters of the same rank are free among themselves");
    }

    private static void AssertIds(in ImmutableArray<Diagnostic> diagnostics, string message)
    {
        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB1008"]), message);
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string action)
    {
        return AnalyzerTestHost.Analyze(new ActionParameterOrderAnalyzer(), Fixture(action));
    }

    private static string Fixture(string action)
    {
        return $$"""
            using System;
            using System.Threading;
            using Microsoft.AspNetCore.Mvc;

            namespace FakeApi;

            public interface IThing { }

            [ApiController]
            [Route("api/items")]
            public class ItemsController : ControllerBase
            {
            {{action}}
            }
            """;
    }
}
