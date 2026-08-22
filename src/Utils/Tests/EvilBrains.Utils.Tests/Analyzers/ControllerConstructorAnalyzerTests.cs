using System.Collections.Immutable;
using EvilBrains.Analyzers;
using EvilBrains.Utils.Tests.ApiClient;
using Microsoft.CodeAnalysis;

namespace EvilBrains.Utils.Tests.Analyzers;

public class ControllerConstructorAnalyzerTests
{
    [Test]
    public async Task ControllerWithoutConstructorHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            [ApiController]
            [Route("api/items")]
            public class ItemsController : ControllerBase
            {
                [HttpGet("")]
                public string GetItems([FromServices] IThing thing) => thing.Name;
            }
            """);

        Assert.That(diagnostics, Is.Empty, "a [FromServices] action parameter is the way to take a dependency");
    }

    [Test]
    public async Task PrimaryConstructorDependencyIsReportedTest()
    {
        var diagnostics = await Analyze("""
            [ApiController]
            [Route("api/items")]
            public class ItemsController(IThing thing) : ControllerBase
            {
                [HttpGet("")]
                public string GetItems() => thing.Name;
            }
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0007"]), "a controller must not take constructor dependencies");
        Assert.That(diagnostics.Single().GetMessage(CultureInfo.InvariantCulture), Does.Contain("thing"), "a controller must not take constructor dependencies");
    }

    [Test]
    public async Task DeclaredConstructorDependencyIsReportedTest()
    {
        var diagnostics = await Analyze("""
            [ApiController]
            [Route("api/items")]
            public class ItemsController : ControllerBase
            {
                private readonly IThing thing;

                public ItemsController(IThing thing) => this.thing = thing;

                [HttpGet("")]
                public string GetItems() => this.thing.Name;
            }
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0007"]), "a controller must not take constructor dependencies");
    }

    [Test]
    public async Task EveryConstructorParameterIsReportedTest()
    {
        var diagnostics = await Analyze("""
            [ApiController]
            [Route("api/items")]
            public class ItemsController(IThing first, IThing second) : ControllerBase
            {
                [HttpGet("")]
                public string GetItems() => first.Name + second.Name;
            }
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0007", "EB0007"]), "each constructor dependency is reported");
    }

    [Test]
    public async Task TypeOutsideMvcIsIgnoredTest()
    {
        var diagnostics = await Analyze("""
            public class ItemsService(IThing thing)
            {
                public string Name() => thing.Name;
            }

            public class NotesController(IThing thing)
            {
                public string Name() => thing.Name;
            }
            """);

        Assert.That(diagnostics, Is.Empty, "the rule follows ControllerBase, not the type name");
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string type) =>
        AnalyzerTestHost.Analyze(new ControllerConstructorAnalyzer(), Fixture(type));

    private static string Fixture(string type) => $$"""
        using Microsoft.AspNetCore.Mvc;

        namespace FakeApi;

        public interface IThing
        {
            string Name { get; }
        }

        {{type}}
        """;
}
