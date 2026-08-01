using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.Utils.Tests.ApiClient;

internal static class AnalyzerTestHost
{
    public static async Task<ImmutableArray<Diagnostic>> Analyze(DiagnosticAnalyzer analyzer, string source)
    {
        var compilation = TestCompilation.Create("FakeApi", source);
        var errors = compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error).ToList();
        Assert.That(errors, Is.Empty, "analyzer fixture source must compile");

        var withAnalyzers = compilation.WithAnalyzers([analyzer]);

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None);
    }
}
