using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EvilBrains.ApiClient.Generator;

/// <summary>
/// Syntactic attribute helpers: controller sources are parsed without ASP.NET references,
/// so MVC attributes never resolve semantically and must be matched by name.
/// </summary>
internal static class AttributeFacts
{
    private const string AttributeSuffix = "Attribute";

    public static string GetName(AttributeSyntax attribute)
    {
        var name = attribute.Name;
        while (name is QualifiedNameSyntax qualified)
            name = qualified.Right;

        var text = name is IdentifierNameSyntax identifier ? identifier.Identifier.Text : name.ToString();

        return text.EndsWith(AttributeSuffix, StringComparison.Ordinal) ? text.Substring(0, text.Length - AttributeSuffix.Length) : text;
    }

    public static AttributeSyntax? Find(in SyntaxList<AttributeListSyntax> lists, string name)
    {
        return lists.SelectMany(static x => x.Attributes).FirstOrDefault(x => string.Equals(GetName(x), name, StringComparison.Ordinal));
    }

    public static bool Has(in SyntaxList<AttributeListSyntax> lists, string name)
    {
        return Find(lists, name) is not null;
    }

    public static string? GetTemplateArgument(AttributeSyntax attribute, SemanticModel semanticModel)
    {
        var argument = attribute.ArgumentList?.Arguments.FirstOrDefault(static x =>
            x.NameEquals is null && (x.NameColon is null || string.Equals(x.NameColon.Name.Identifier.Text, "template", StringComparison.Ordinal)));
        if (argument is null)
            return null;

        return semanticModel.GetConstantValue(argument.Expression).Value as string;
    }

    public static string? GetNameArgument(AttributeSyntax attribute)
    {
        var argument = attribute.ArgumentList?.Arguments.FirstOrDefault(static x => string.Equals(x.NameEquals?.Name.Identifier.Text, "Name", StringComparison.Ordinal));

        return argument?.Expression is LiteralExpressionSyntax { Token.Value: string value } ? value : null;
    }
}
