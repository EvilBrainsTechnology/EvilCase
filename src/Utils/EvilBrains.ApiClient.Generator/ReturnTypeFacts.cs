using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EvilBrains.ApiClient.Generator;

/// <summary>
/// Unwraps action return types syntactically: MVC result types do not resolve in the client
/// compilation, so wrappers are matched by name and only the innermost type is bound semantically.
/// </summary>
internal static class ReturnTypeFacts
{
    private static readonly ImmutableHashSet<string> ResultWrappers = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Task",
        "ValueTask",
        "ActionResult");

    private static readonly ImmutableHashSet<string> EmptyResults = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Task",
        "ValueTask",
        "ActionResult",
        "IActionResult");

    public static TypeSyntax? Peel(TypeSyntax type)
    {
        var current = type;

        while (true)
        {
            if (current is PredefinedTypeSyntax predefined)
                return predefined.Keyword.IsKind(SyntaxKind.VoidKeyword) ? null : current;

            if (current is QualifiedNameSyntax qualified)
            {
                current = qualified.Right;

                continue;
            }

            if (current is AliasQualifiedNameSyntax alias)
            {
                current = alias.Name;

                continue;
            }

            if (current is GenericNameSyntax generic && generic.TypeArgumentList.Arguments.Count == 1 && ResultWrappers.Contains(generic.Identifier.Text))
            {
                current = generic.TypeArgumentList.Arguments[0];

                continue;
            }

            if (current is IdentifierNameSyntax identifier && EmptyResults.Contains(identifier.Identifier.Text))
                return null;

            return current;
        }
    }
}
