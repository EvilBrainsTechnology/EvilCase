using System.Collections.Immutable;
using EvilBrains.Analyzers;
using EvilBrains.Utils.Tests.ApiClient;
using Microsoft.CodeAnalysis;

namespace EvilBrains.Utils.Tests.Analyzers;

public class ExpressionBodyAnalyzerTests
{
    [Test]
    public async Task ExpressionBodiedMethodIsReportedTest()
    {
        var diagnostics = await Analyze("""
            public int Value() => 1;
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0006"]), "member should have a block body");
    }

    [Test]
    public async Task BlockBodiedMethodHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public int Value() { return 1; }
            """);

        Assert.That(diagnostics, Is.Empty, "a block-bodied method must not be reported");
    }

    [Test]
    public async Task SingleLinePropertyHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public int Value => 1;
            """);

        Assert.That(diagnostics, Is.Empty, "a single-line property may use an expression body");
    }

    [Test]
    public async Task MultiLinePropertyIsReportedTest()
    {
        var diagnostics = await Analyze("""
            public int Value =>
                1;
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0006"]), "a property spanning multiple lines must have a block body");
    }

    [Test]
    public async Task PropertyWithAttributeOnItsOwnLineHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            [System.Obsolete("gone")]
            public int Value => 1;
            """);

        Assert.That(diagnostics, Is.Empty, "the attribute list must not count toward the single-line check");
    }

    [Test]
    public async Task SingleLineIndexerHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public int this[int index] => index;
            """);

        Assert.That(diagnostics, Is.Empty, "a single-line indexer may use an expression body");
    }

    [Test]
    public async Task ExpressionBodiedConstructorIsReportedTest()
    {
        var diagnostics = await Analyze("""
            private readonly int value;
            public Sample(int value) => this.value = value;
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0006"]), "a constructor must have a block body");
    }

    [Test]
    public async Task ExpressionBodiedOperatorIsReportedTest()
    {
        var diagnostics = await Analyze("""
            public static Sample operator +(Sample left, Sample right) => left;
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0006"]), "an operator must have a block body");
    }

    [Test]
    public async Task ExpressionBodiedLocalFunctionIsReportedTest()
    {
        var diagnostics = await Analyze("""
            public int Value()
            {
                int Inner() => 1;
                return Inner();
            }
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0006"]), "a local function must have a block body");
    }

    [Test]
    public async Task LambdaHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public int Value()
            {
                System.Func<int, int> add = x => x + 1;
                return add(1);
            }
            """);

        Assert.That(diagnostics, Is.Empty, "a lambda expression is out of scope for the analyzer");
    }

    [Test]
    public async Task ExpressionBodiedAccessorHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public int Value
            {
                get => 1;
            }
            """);

        Assert.That(diagnostics, Is.Empty, "an accessor body is out of scope for the analyzer");
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string body)
    {
        return AnalyzerTestHost.Analyze(new ExpressionBodyAnalyzer(), Fixture(body));
    }

    private static string Fixture(string body)
    {
        return $$"""
            namespace Fake;

            public class Sample
            {
            {{body}}
            }
            """;
    }
}
