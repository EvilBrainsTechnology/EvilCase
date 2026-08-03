using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.ApiClient.Generator;

/// <summary>
/// Enforces explicit parameter binding on [ApiController] actions: every parameter carries
/// exactly one binding attribute, except CancellationToken which carries none.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ActionParameterBindingAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Diagnostics.MissingBindingAttribute];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (!MvcFacts.IsApiController(type))
            return;

        foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(MvcFacts.IsAction))
        {
            foreach (var parameter in method.Parameters)
                AnalyzeParameter(context, parameter);
        }
    }

    private static void AnalyzeParameter(in SymbolAnalysisContext context, IParameterSymbol parameter)
    {
        var bindings = parameter.GetAttributes().Count(MvcFacts.IsBindingAttribute);
        var expected = TypeFacts.IsCancellationToken(parameter.Type) ? 0 : 1;

        if (bindings != expected)
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingBindingAttribute, parameter.Locations[0], parameter.Name));
    }
}
