using System.Collections.Immutable;
using EvilBrains.Analyzers;
using EvilBrains.Utils.Tests.ApiClient;
using Microsoft.CodeAnalysis;

namespace EvilBrains.Utils.Tests.Analyzers;

public class WhereConjunctionAnalyzerTests
{
    [Test]
    public async Task AndInsideWhereIsReportedTest()
    {
        var diagnostics = await Analyze("""
            public IEnumerable<int> Filter(IEnumerable<int> numbers)
            {
                return numbers.Where(number => number > 1 && number < 5);
            }
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0010"]), "consecutive Where calls are the form, not '&&'");
    }

    [Test]
    public async Task ThreeConditionsAreReportedOnceTest()
    {
        var diagnostics = await Analyze("""
            public IEnumerable<int> Filter(IEnumerable<int> numbers)
            {
                return numbers.Where(number => number > 1 && number < 5 && number != 3);
            }
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0010"]), "one Where with '&&' is one finding, however many conditions it joins");
    }

    [Test]
    public async Task ParenthesizedPredicateIsReportedTest()
    {
        var diagnostics = await Analyze("""
            public IEnumerable<int> Filter(IEnumerable<int> numbers)
            {
                return numbers.Where(number => (number > 1 && number < 5));
            }
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0010"]), "parentheses around the predicate must not hide the '&&'");
    }

    [Test]
    public async Task AndInAQueryableWhereIsReportedTest()
    {
        var diagnostics = await Analyze("""
            public IQueryable<int> Filter(IQueryable<int> numbers)
            {
                return numbers.Where(number => number > 1 && number < 5);
            }
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0010"]), "a queryable Where carries the same form");
    }

    [Test]
    public async Task ConsecutiveWhereCallsHaveNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public IEnumerable<int> Filter(IEnumerable<int> numbers)
            {
                return numbers.Where(number => number > 1).Where(number => number < 5);
            }
            """);

        Assert.That(diagnostics, Is.Empty, "consecutive Where calls are the form the rule asks for");
    }

    [Test]
    public async Task AndUnderAnOrHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public IEnumerable<int> Filter(IEnumerable<int> numbers)
            {
                return numbers.Where(number => number > 10 || (number > 1 && number < 5));
            }
            """);

        Assert.That(diagnostics, Is.Empty, "an '&&' under an '||' is one rule and cannot be split");
    }

    [Test]
    public async Task AndInANestedLambdaHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public IEnumerable<int> Filter(IEnumerable<int> numbers, IEnumerable<int> others)
            {
                return numbers.Where(number => others.Any(other => other > 1 && other < 5));
            }
            """);

        Assert.That(diagnostics, Is.Empty, "only the Where predicate itself is in scope");
    }

    [Test]
    public async Task AndInAnotherLinqMethodHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public bool HasMatch(IEnumerable<int> numbers)
            {
                return numbers.Any(number => number > 1 && number < 5);
            }
            """);

        Assert.That(diagnostics, Is.Empty, "only Where is in scope");
    }

    [Test]
    public async Task WhereOnAnotherTypeHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public class Filter
            {
                public Filter Where(System.Func<int, bool> predicate)
                {
                    return this;
                }
            }

            public Filter Narrow(Filter filter)
            {
                return filter.Where(value => value > 1 && value < 5);
            }
            """);

        Assert.That(diagnostics, Is.Empty, "only the LINQ Where is in scope");
    }

    [Test]
    public async Task BlockBodiedPredicateHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public IEnumerable<int> Filter(IEnumerable<int> numbers)
            {
                return numbers.Where(number =>
                {
                    return number > 1 && number < 5;
                });
            }
            """);

        Assert.That(diagnostics, Is.Empty, "a block-bodied predicate is out of scope for the analyzer");
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string body)
    {
        return AnalyzerTestHost.Analyze(new WhereConjunctionAnalyzer(), Fixture(body));
    }

    private static string Fixture(string body)
    {
        return $$"""
            using System.Collections.Generic;
            using System.Linq;

            namespace Fake;

            public class Sample
            {
            {{body}}
            }
            """;
    }
}
