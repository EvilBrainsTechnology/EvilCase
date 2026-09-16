using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace EvilBrains.ApiClient.Generator;

internal static class TypeFacts
{
    private static readonly SymbolDisplayFormat TypeFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly ImmutableHashSet<SpecialType> SimpleSpecialTypes = ImmutableHashSet.Create(
        SpecialType.System_String,
        SpecialType.System_Char,
        SpecialType.System_Boolean,
        SpecialType.System_SByte,
        SpecialType.System_Byte,
        SpecialType.System_Int16,
        SpecialType.System_UInt16,
        SpecialType.System_Int32,
        SpecialType.System_UInt32,
        SpecialType.System_Int64,
        SpecialType.System_UInt64,
        SpecialType.System_Single,
        SpecialType.System_Double,
        SpecialType.System_Decimal);

    private static readonly ImmutableHashSet<string> SimpleTypeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "System.Guid",
        "System.DateTime",
        "System.DateTimeOffset",
        "System.DateOnly",
        "System.TimeOnly",
        "System.TimeSpan");

    public static string Display(ITypeSymbol type)
    {
        return type.ToDisplayString(TypeFormat);
    }

    public static bool IsCancellationToken(ITypeSymbol type)
    {
        return string.Equals(type.ToDisplayString(), "System.Threading.CancellationToken", StringComparison.Ordinal);
    }

    public static bool IsNullable(ITypeSymbol type)
    {
        return type.NullableAnnotation == NullableAnnotation.Annotated || IsNullableValue(type);
    }

    public static bool IsNullableValue(ITypeSymbol type)
    {
        return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    public static ITypeSymbol Unwrap(ITypeSymbol type)
    {
        return IsNullableValue(type) ? ((INamedTypeSymbol)type).TypeArguments[0] : type;
    }

    public static bool ContainsError(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Error)
            return true;

        return type is INamedTypeSymbol named && named.TypeArguments.Any(ContainsError);
    }

    /// <summary>
    /// An array or an IReadOnlyList of a simple element; the wire carries one query pair per value.
    /// </summary>
    public static bool IsSimpleCollection(ITypeSymbol type)
    {
        var element = Element(type);

        return element is not null && IsSimple(element);
    }

    public static bool IsSimple(ITypeSymbol type)
    {
        var unwrapped = Unwrap(type);
        if (unwrapped.TypeKind == TypeKind.Enum)
            return true;

        if (SimpleSpecialTypes.Contains(unwrapped.SpecialType))
            return true;

        return SimpleTypeNames.Contains(unwrapped.ToDisplayString());
    }

    private static ITypeSymbol? Element(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
            return array.ElementType;

        return type is INamedTypeSymbol { IsGenericType: true } named
            && string.Equals(named.ConstructedFrom.ToDisplayString(), "System.Collections.Generic.IReadOnlyList<T>", StringComparison.Ordinal)
                ? named.TypeArguments[0]
                : null;
    }
}
