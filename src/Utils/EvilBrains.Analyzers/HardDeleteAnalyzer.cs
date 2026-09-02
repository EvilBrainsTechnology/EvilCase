using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.Analyzers;

/// <summary>
/// ExecuteDelete runs outside every interceptor, so nothing can turn it into the stamp an
/// ISoftDeleteEntity is deleted by. It is the one delete the entity has to refuse at the call site.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HardDeleteAnalyzer : DiagnosticAnalyzer
{
    private const string SoftDeleteEntityName = "ISoftDeleteEntity";

    private static readonly DiagnosticDescriptor HardDelete = new(
        "EB0011",
        "Soft-delete entity is removed by ExecuteDelete",
        "Stamp the entity with ExecuteSoftDelete; ExecuteDelete takes the rows for good",
        "EvilBrains.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [HardDelete];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
            return;

        if (!IsExecuteDelete(method))
            return;

        var source = method.TypeArguments.FirstOrDefault();

        if (!IsSoftDeleteEntity(source))
            return;

        context.ReportDiagnostic(Diagnostic.Create(HardDelete, invocation.GetLocation()));
    }

    private static bool IsExecuteDelete(IMethodSymbol method)
    {
        return string.Equals(method.Name, "ExecuteDelete", StringComparison.Ordinal)
            || string.Equals(method.Name, "ExecuteDeleteAsync", StringComparison.Ordinal);
    }

    private static bool IsSoftDeleteEntity(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        return type.AllInterfaces.Any(static contract => string.Equals(contract.Name, SoftDeleteEntityName, StringComparison.Ordinal));
    }
}
