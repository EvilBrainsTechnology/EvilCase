using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace EvilBrains.Analyzers;

/// <summary>
/// The one definition of a controller and its actions, shared by both analyzer assemblies.
/// </summary>
public static class MvcFacts
{
    private const string ApiControllerAttributeName = "Microsoft.AspNetCore.Mvc.ApiControllerAttribute";

    private const string RouteAttributeName = "Microsoft.AspNetCore.Mvc.RouteAttribute";

    private const string HttpMethodAttributeName = "Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute";

    private const string NonActionAttributeName = "Microsoft.AspNetCore.Mvc.NonActionAttribute";

    private static readonly ImmutableHashSet<string> BindingAttributeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Microsoft.AspNetCore.Mvc.FromBodyAttribute",
        "Microsoft.AspNetCore.Mvc.FromQueryAttribute",
        "Microsoft.AspNetCore.Mvc.FromRouteAttribute",
        "Microsoft.AspNetCore.Mvc.FromHeaderAttribute",
        "Microsoft.AspNetCore.Mvc.FromFormAttribute",
        "Microsoft.AspNetCore.Mvc.FromServicesAttribute",
        "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute");

    public static bool IsApiController(INamedTypeSymbol type)
    {
        return HasAttribute(type, ApiControllerAttributeName);
    }

    public static bool IsAction(IMethodSymbol method)
    {
        if (method is not { MethodKind: MethodKind.Ordinary, DeclaredAccessibility: Accessibility.Public, IsStatic: false })
            return false;

        return !HasAttribute(method, NonActionAttributeName);
    }

    public static AttributeData? FindRouteAttribute(INamedTypeSymbol type)
    {
        return type.GetAttributes().FirstOrDefault(x => string.Equals(GetAttributeName(x), RouteAttributeName, StringComparison.Ordinal));
    }

    public static bool IsHttpMethodAttribute(AttributeData attribute)
    {
        for (var type = attribute.AttributeClass; type is not null; type = type.BaseType)
        {
            if (string.Equals(type.ToDisplayString(), HttpMethodAttributeName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static bool IsBindingAttribute(AttributeData attribute)
    {
        var name = GetAttributeName(attribute);

        return name is not null && BindingAttributeNames.Contains(name);
    }

    public static string? GetTemplate(AttributeData attribute)
    {
        return attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string : null;
    }

    public static Location GetLocation(AttributeData attribute, ISymbol fallback)
    {
        return attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? fallback.Locations[0];
    }

    private static string? GetAttributeName(AttributeData attribute)
    {
        return attribute.AttributeClass?.ToDisplayString();
    }

    private static bool HasAttribute(ISymbol symbol, string name)
    {
        return symbol.GetAttributes().Any(x => string.Equals(GetAttributeName(x), name, StringComparison.Ordinal));
    }
}
