using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.Analyzers;

/// <summary>
/// Keeps controllers free of constructor dependencies; an action takes what it needs as a [FromServices] parameter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ControllerConstructorAnalyzer : DiagnosticAnalyzer
{
    private const string ControllerBaseTypeName = "Microsoft.AspNetCore.Mvc.ControllerBase";

    private static readonly DiagnosticDescriptor NoConstructorDependencies = new(
        "EB0007",
        "Controller must not take constructor dependencies",
        "Controller '{0}' takes '{1}' in its constructor; an action takes it as a [FromServices] parameter",
        "EvilBrains.Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [NoConstructorDependencies];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (!IsController(type))
            return;

        foreach (var constructor in type.InstanceConstructors.Where(x => !x.IsImplicitlyDeclared))
        {
            foreach (var parameter in constructor.Parameters)
                context.ReportDiagnostic(Diagnostic.Create(NoConstructorDependencies, parameter.Locations[0], type.Name, parameter.Name));
        }
    }

    private static bool IsController(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (string.Equals(current.ToDisplayString(), ControllerBaseTypeName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
