using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UsingDirectiveOrderAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Ordered = new(
        "EB0004",
        "Using directives should be ordered",
        "Using directives should be ordered: System namespaces first, then other namespaces alphabetically, then using static, then aliases",
        "EvilBrains.Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Ordered];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.CompilationUnit, SyntaxKind.NamespaceDeclaration, SyntaxKind.FileScopedNamespaceDeclaration);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var usings = context.Node switch
        {
            CompilationUnitSyntax unit => unit.Usings,
            BaseNamespaceDeclarationSyntax ns => ns.Usings,
            _ => default,
        };

        for (var i = 1; i < usings.Count; i++)
        {
            if (Compare(usings[i - 1], usings[i]) > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Ordered, usings[i].GetLocation()));

                return;
            }
        }
    }

    private static int Compare(UsingDirectiveSyntax first, UsingDirectiveSyntax second)
    {
        var rankDifference = GetRank(first) - GetRank(second);

        return rankDifference != 0 ? rankDifference : string.CompareOrdinal(GetSortName(first), GetSortName(second));
    }

    private static int GetRank(UsingDirectiveSyntax directive)
    {
        if (directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
            return 0;

        if (directive.Alias is not null)
            return 4;

        if (directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
            return 3;

        return IsSystemNamespace(directive.Name?.ToString() ?? "") ? 1 : 2;
    }

    private static string GetSortName(UsingDirectiveSyntax directive)
    {
        return directive.Alias is not null ? directive.Alias.Name.Identifier.Text : directive.Name?.ToString() ?? "";
    }

    private static bool IsSystemNamespace(string name)
    {
        return string.Equals(name, "System", StringComparison.Ordinal) || name.StartsWith("System.", StringComparison.Ordinal);
    }
}
