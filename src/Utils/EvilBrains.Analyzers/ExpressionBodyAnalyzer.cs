using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.Analyzers;

/// <summary>
/// Requires a block body on every member; an expression body is allowed only on a property or indexer
/// whose declaration fits on one line.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExpressionBodyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor BlockBody = new(
        "EB0006",
        "Member should have a block body",
        "Member should have a block body; only a property or indexer declared on a single line may use an expression body",
        "EvilBrains.Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [BlockBody];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeNode,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.DestructorDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.ConversionOperatorDeclaration,
            SyntaxKind.LocalFunctionStatement,
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.IndexerDeclaration);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var body = GetExpressionBody(context.Node);

        if (body is null)
            return;

        if (context.Node is PropertyDeclarationSyntax or IndexerDeclarationSyntax && IsOnOneLine(context.Node, context.CancellationToken))
            return;

        context.ReportDiagnostic(Diagnostic.Create(BlockBody, body.ArrowToken.GetLocation()));
    }

    private static ArrowExpressionClauseSyntax? GetExpressionBody(SyntaxNode node)
    {
        return node switch
        {
            BaseMethodDeclarationSyntax x => x.ExpressionBody,
            LocalFunctionStatementSyntax x => x.ExpressionBody,
            PropertyDeclarationSyntax x => x.ExpressionBody,
            IndexerDeclarationSyntax x => x.ExpressionBody,
            _ => null,
        };
    }

    private static bool IsOnOneLine(SyntaxNode node, CancellationToken token)
    {
        var text = node.SyntaxTree.GetText(token);
        var start = GetStartAfterAttributes(node);

        return text.Lines.GetLineFromPosition(start).LineNumber == text.Lines.GetLineFromPosition(node.Span.End).LineNumber;
    }

    private static int GetStartAfterAttributes(SyntaxNode node)
    {
        foreach (var child in node.ChildNodesAndTokens())
        {
            if (!child.IsKind(SyntaxKind.AttributeList))
                return child.Span.Start;
        }

        return node.SpanStart;
    }
}
