using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.ApiClient.Generator;

/// <summary>
/// Keeps controllers free of constructor dependencies; an action takes what it needs as a [FromServices] parameter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ControllerConstructorAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Diagnostics.ControllerConstructorDependency];

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

        foreach (var constructor in type.InstanceConstructors.Where(x => !x.IsImplicitlyDeclared))
        {
            foreach (var parameter in constructor.Parameters)
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ControllerConstructorDependency, parameter.Locations[0], type.Name, parameter.Name));
        }
    }
}
