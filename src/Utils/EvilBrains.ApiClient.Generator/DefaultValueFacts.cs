using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EvilBrains.ApiClient.Generator;

/// <summary>
/// Formats parameter default values from symbols: the source text may reference names (enum members,
/// constants) that do not resolve in the generated code, so the constant value is re-emitted instead.
/// </summary>
internal static class DefaultValueFacts
{
    public static string? Format(IParameterSymbol symbol)
    {
        if (!symbol.HasExplicitDefaultValue)
            return null;

        var value = symbol.ExplicitDefaultValue;
        if (value is null)
            return symbol.Type.IsValueType && !TypeFacts.IsNullableValue(symbol.Type) ? "default" : "null";

        var type = TypeFacts.Unwrap(symbol.Type);
        if (type.TypeKind == TypeKind.Enum)
            return "(" + TypeFacts.Display(type) + ")" + Convert.ToString(value, CultureInfo.InvariantCulture);

        return value switch
        {
            string text => SymbolDisplay.FormatLiteral(text, quote: true),
            char character => SymbolDisplay.FormatLiteral(character, quote: true),
            bool flag => flag ? "true" : "false",
            float single => single.ToString("R", CultureInfo.InvariantCulture) + "f",
            double number => number.ToString("R", CultureInfo.InvariantCulture) + "d",
            decimal number => number.ToString(CultureInfo.InvariantCulture) + "m",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "default",
        };
    }
}
