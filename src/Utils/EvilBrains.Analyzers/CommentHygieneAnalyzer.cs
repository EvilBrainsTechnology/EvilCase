using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace EvilBrains.Analyzers;

/// <summary>
/// Enforces single-line comment hygiene previously covered by StyleCop rules SA1005, SA1515 and SA1512.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommentHygieneAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor BeginWithSpace = new(
        "EB0001",
        "Single-line comment should begin with a space",
        "Single-line comment should begin with a space",
        "EvilBrains.Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor PrecededByBlankLine = new(
        "EB0002",
        "Single-line comment should be preceded by a blank line",
        "Single-line comment should be preceded by a blank line",
        "EvilBrains.Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NotFollowedByBlankLine = new(
        "EB0003",
        "Single-line comment should not be followed by a blank line",
        "Single-line comment should not be followed by a blank line",
        "EvilBrains.Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [BeginWithSpace, PrecededByBlankLine, NotFollowedByBlankLine];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var text = context.Tree.GetText(context.CancellationToken);

        foreach (var trivia in root.DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                AnalyzeComment(context, text, trivia);
        }
    }

    private static void AnalyzeComment(in SyntaxTreeAnalysisContext context, SourceText text, in SyntaxTrivia trivia)
    {
        var comment = trivia.ToString();
        var content = comment.Substring(2);

        // Comments starting with an extra slash ("////") mark commented-out code and are exempt, matching StyleCop behavior.
        var isCommentedOutCode = content.Length > 0 && content[0] == '/';

        if (!isCommentedOutCode && content.Length > 0 && content[0] != ' ' && content[0] != '\t')
            context.ReportDiagnostic(Diagnostic.Create(BeginWithSpace, trivia.GetLocation()));

        var line = text.Lines.GetLineFromPosition(trivia.SpanStart);

        if (isCommentedOutCode || !IsLineLeading(text, line, trivia.SpanStart))
            return;

        if (!IsAllowedPredecessor(text, line.LineNumber))
            context.ReportDiagnostic(Diagnostic.Create(PrecededByBlankLine, trivia.GetLocation()));

        if (IsFollowedByBlankLine(text, line.LineNumber) && !IsTopOfFileComment(text, line.LineNumber))
            context.ReportDiagnostic(Diagnostic.Create(NotFollowedByBlankLine, trivia.GetLocation()));
    }

    private static bool IsLineLeading(SourceText text, in TextLine line, int position)
    {
        for (var i = line.Start; i < position; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
                return false;
        }

        return true;
    }

    private static bool IsAllowedPredecessor(SourceText text, int lineNumber)
    {
        if (lineNumber == 0)
            return true;

        var previous = text.Lines[lineNumber - 1].ToString().Trim();

        return previous.Length == 0
            || previous.StartsWith("//", StringComparison.Ordinal)
            || previous.StartsWith("#", StringComparison.Ordinal)
            || previous.EndsWith("{", StringComparison.Ordinal)
            || previous.EndsWith(":", StringComparison.Ordinal);
    }

    private static bool IsFollowedByBlankLine(SourceText text, int lineNumber)
    {
        return lineNumber + 1 < text.Lines.Count
            && text.Lines[lineNumber + 1].ToString().Trim().Length == 0;
    }

    private static bool IsTopOfFileComment(SourceText text, int lineNumber)
    {
        for (var i = lineNumber - 1; i >= 0; i--)
        {
            var value = text.Lines[i].ToString().Trim();

            if (value.Length > 0 && !value.StartsWith("//", StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
