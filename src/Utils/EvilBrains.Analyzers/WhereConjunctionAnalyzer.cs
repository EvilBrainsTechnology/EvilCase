using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WhereConjunctionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Conjunction = new(
        "EB0010",
        "Where predicate joins conditions with '&&'",
        "Split the Where predicate into consecutive Where calls, one per condition",
        "EvilBrains.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Conjunction];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var arguments = invocation.ArgumentList.Arguments;

        if (!arguments.Any())
            return;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method || !IsLinqWhere(method))
            return;

        var body = GetSinglePredicateBody(arguments.Last().Expression);

        if (body is null)
            return;

        var conjunction = FindConjunction(body);

        if (conjunction is not null)
            context.ReportDiagnostic(Diagnostic.Create(Conjunction, conjunction.OperatorToken.GetLocation()));
    }

    private static bool IsLinqWhere(IMethodSymbol method)
    {
        if (!string.Equals(method.Name, "Where", StringComparison.Ordinal))
            return false;

        var containingType = method.ContainingType?.ToDisplayString();

        return string.Equals(containingType, "System.Linq.Enumerable", StringComparison.Ordinal)
            || string.Equals(containingType, "System.Linq.Queryable", StringComparison.Ordinal);
    }

    private static ExpressionSyntax? GetSinglePredicateBody(ExpressionSyntax predicate)
    {
        return predicate switch
        {
            SimpleLambdaExpressionSyntax lambda => lambda.ExpressionBody,
            ParenthesizedLambdaExpressionSyntax lambda when lambda.ParameterList.Parameters.Count == 1 => lambda.ExpressionBody,
            _ => null,
        };
    }

    // Only a top-level '&&': one under an '||' is one rule and stays.
    private static BinaryExpressionSyntax? FindConjunction(ExpressionSyntax expression)
    {
        return expression switch
        {
            ParenthesizedExpressionSyntax parenthesized => FindConjunction(parenthesized.Expression),
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression) => binary,
            _ => null,
        };
    }
}
