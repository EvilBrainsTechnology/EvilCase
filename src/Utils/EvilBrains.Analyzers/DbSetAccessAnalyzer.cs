using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.Analyzers;

/// <summary>
/// A typed DbSet names the entity every read and write reaches for; Set&lt;TEntity&gt;() belongs to the
/// context's own declaration and nowhere else.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DbSetAccessAnalyzer : DiagnosticAnalyzer
{
    private const string DbContextName = "Microsoft.EntityFrameworkCore.DbContext";

    private static readonly DiagnosticDescriptor UntypedSet = new(
        "EB0009",
        "Entity is reached through Set<TEntity>() instead of its typed DbSet",
        "Reach the entity through its typed DbSet; Set<TEntity>() belongs to the context's own declaration",
        "EvilBrains.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [UntypedSet];

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

        if (!string.Equals(method.Name, "Set", StringComparison.Ordinal) || !IsDbContext(method.ContainingType))
            return;

        if (IsInsideDbContext(context.ContainingSymbol))
            return;

        context.ReportDiagnostic(Diagnostic.Create(UntypedSet, invocation.GetLocation()));
    }

    private static bool IsInsideDbContext(ISymbol? symbol)
    {
        for (var type = symbol as INamedTypeSymbol ?? symbol?.ContainingType; type is not null; type = type.ContainingType)
        {
            if (IsDbContext(type))
                return true;
        }

        return false;
    }

    private static bool IsDbContext(INamedTypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (string.Equals(current.ToDisplayString(), DbContextName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
