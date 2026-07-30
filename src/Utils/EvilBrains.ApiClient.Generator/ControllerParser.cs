using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EvilBrains.ApiClient.Generator;

internal static class ControllerParser
{
    private const string RouteAttributeName = "Route";

    private const string NonActionAttributeName = "NonAction";

    private static readonly Dictionary<string, string> HttpMethodAttributes = new(StringComparer.Ordinal)
    {
        ["HttpGet"] = "Get",
        ["HttpPost"] = "Post",
        ["HttpPut"] = "Put",
        ["HttpDelete"] = "Delete",
        ["HttpPatch"] = "Patch",
        ["HttpHead"] = "Head",
        ["HttpOptions"] = "Options",
    };

    private static readonly ImmutableHashSet<string> BindingAttributeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "FromBody",
        "FromQuery",
        "FromRoute",
        "FromHeader",
        "FromForm",
        "FromServices",
        "FromKeyedServices");

    public static ClientModel? Parse(ClassDeclarationSyntax controller, SemanticModel semanticModel, string name, ImmutableArray<DiagnosticModel>.Builder diagnostics)
    {
        var routeAttribute = AttributeFacts.Find(controller.AttributeLists, RouteAttributeName);
        var controllerRoute = routeAttribute is null ? null : AttributeFacts.GetTemplateArgument(routeAttribute, semanticModel);
        if (controllerRoute is null)
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.MissingControllerRoute, controller.Identifier, controller.Identifier.Text));

            return null;
        }

        if (!ValidateTemplate(routeAttribute!, controllerRoute, diagnostics))
            return null;

        var actions = ImmutableArray.CreateBuilder<ActionModel>();
        var valid = true;

        foreach (var method in controller.Members.OfType<MethodDeclarationSyntax>().Where(IsAction))
        {
            var action = ParseAction(method, semanticModel, controllerRoute, diagnostics);
            if (action is null)
            {
                valid = false;

                continue;
            }

            actions.Add(action);
        }

        return valid ? new(name, new(actions.ToImmutable())) : null;
    }

    private static bool IsAction(MethodDeclarationSyntax method)
    {
        if (!method.Modifiers.Any(SyntaxKind.PublicKeyword) || method.Modifiers.Any(SyntaxKind.StaticKeyword))
            return false;

        return !AttributeFacts.Has(method.AttributeLists, NonActionAttributeName);
    }

    private static ActionModel? ParseAction(MethodDeclarationSyntax method, SemanticModel semanticModel, string controllerRoute, ImmutableArray<DiagnosticModel>.Builder diagnostics)
    {
        var verbs = FindVerbs(method);
        var template = verbs.Count == 1 ? AttributeFacts.GetTemplateArgument(verbs[0].Attribute, semanticModel) : null;
        if (template is null)
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.MissingActionRoute, method.Identifier, method.Identifier.Text));

            return null;
        }

        if (!ValidateTemplate(verbs[0].Attribute, template, diagnostics))
            return null;

        if (semanticModel.GetDeclaredSymbol(method) is not IMethodSymbol symbol)
            return null;

        var result = ParseResult(method, semanticModel, diagnostics);
        if (result is null)
            return null;

        var parameters = ParseParameters(method, symbol, diagnostics);
        if (parameters is null)
            return null;

        var route = RouteTemplate.Combine(controllerRoute, template);
        if (!ValidatePlaceholders(method, route, parameters.Value, diagnostics))
            return null;

        return new(symbol.Name, verbs[0].Method, route, result.Value.Type, result.Value.IsNullable, new(parameters.Value));
    }

    private static (string? Type, bool IsNullable)? ParseResult(MethodDeclarationSyntax method, SemanticModel semanticModel, ImmutableArray<DiagnosticModel>.Builder diagnostics)
    {
        var resultType = ReturnTypeFacts.Peel(method.ReturnType);
        if (resultType is null)
            return (null, false);

        var type = semanticModel.GetTypeInfo(resultType).Type;
        if (type is null || TypeFacts.ContainsError(type))
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.TypeNotVisibleToClient, resultType, resultType.ToString()));

            return null;
        }

        return (TypeFacts.Display(type), TypeFacts.IsNullable(type));
    }

    private static ImmutableArray<ParameterModel>? ParseParameters(MethodDeclarationSyntax method, IMethodSymbol symbol, ImmutableArray<DiagnosticModel>.Builder diagnostics)
    {
        var parameters = ImmutableArray.CreateBuilder<ParameterModel>();
        var valid = true;
        var syntaxParameters = method.ParameterList.Parameters;

        for (var i = 0; i < syntaxParameters.Count && i < symbol.Parameters.Length; i++)
        {
            var model = ParseParameter(syntaxParameters[i], symbol.Parameters[i], diagnostics);
            if (model is null)
            {
                valid = false;

                continue;
            }

            parameters.Add(model);
        }

        if (parameters.Where(x => x.Kind == ParameterKind.Body).Skip(1).Any() || parameters.Where(x => x.Kind == ParameterKind.Token).Skip(1).Any())
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.DuplicateSpecialParameter, method.Identifier, symbol.Name));
            valid = false;
        }

        return valid ? parameters.ToImmutable() : null;
    }

    private static ParameterModel? ParseParameter(ParameterSyntax syntax, IParameterSymbol symbol, ImmutableArray<DiagnosticModel>.Builder diagnostics)
    {
        if (syntax.Modifiers.Any())
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.UnsupportedParameter, syntax, symbol.Name));

            return null;
        }

        var bindings = FindBindingAttributes(syntax);

        if (TypeFacts.IsCancellationToken(symbol.Type))
        {
            if (bindings.Count > 0)
            {
                diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.MissingBindingAttribute, syntax, symbol.Name));

                return null;
            }

            return Model(symbol, ParameterKind.Token);
        }

        if (bindings.Count != 1)
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.MissingBindingAttribute, syntax, symbol.Name));

            return null;
        }

        var (attribute, binding) = bindings[0];
        if (binding is "FromServices" or "FromKeyedServices")
            return new(symbol.Name, "", ParameterKind.Skipped, symbol.Name, IsNullable: false, DefaultValue: null, QueryProperties: default);

        if (string.Equals(binding, "FromForm", StringComparison.Ordinal))
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.UnsupportedParameter, syntax, symbol.Name));

            return null;
        }

        if (TypeFacts.ContainsError(symbol.Type))
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.TypeNotVisibleToClient, (SyntaxNode?)syntax.Type ?? syntax, syntax.Type?.ToString() ?? symbol.Name));

            return null;
        }

        var wireName = AttributeFacts.GetNameArgument(attribute) ?? symbol.Name;

        return binding switch
        {
            "FromBody" => Model(symbol, ParameterKind.Body),
            "FromRoute" => ParseRouteParameter(syntax, symbol, wireName, diagnostics),
            "FromHeader" => ParseSimpleParameter(syntax, symbol, ParameterKind.Header, wireName, diagnostics),
            "FromQuery" => ParseQueryParameter(syntax, symbol, wireName, diagnostics),
            _ => null,
        };
    }

    private static ParameterModel? ParseRouteParameter(ParameterSyntax syntax, IParameterSymbol symbol, string wireName, ImmutableArray<DiagnosticModel>.Builder diagnostics)
    {
        if (TypeFacts.IsNullable(symbol.Type))
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.ParameterNotSimple, syntax, symbol.Name));

            return null;
        }

        return ParseSimpleParameter(syntax, symbol, ParameterKind.Route, wireName, diagnostics);
    }

    private static ParameterModel? ParseSimpleParameter(ParameterSyntax syntax, IParameterSymbol symbol, ParameterKind kind, string wireName, ImmutableArray<DiagnosticModel>.Builder diagnostics)
    {
        if (!TypeFacts.IsSimple(symbol.Type))
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.ParameterNotSimple, syntax, symbol.Name));

            return null;
        }

        return Model(symbol, kind, wireName);
    }

    private static ParameterModel? ParseQueryParameter(ParameterSyntax syntax, IParameterSymbol symbol, string wireName, ImmutableArray<DiagnosticModel>.Builder diagnostics)
    {
        if (TypeFacts.IsSimple(symbol.Type))
            return Model(symbol, ParameterKind.Query, wireName);

        var properties = ImmutableArray.CreateBuilder<QueryPropertyModel>();

        foreach (var property in GetQueryProperties(symbol.Type))
        {
            if (!TypeFacts.IsSimple(property.Type))
            {
                diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.QueryPropertyNotSimple, syntax, property.Name, TypeFacts.Display(symbol.Type)));

                return null;
            }

            properties.Add(new(property.Name, ToCamelCase(property.Name), TypeFacts.IsNullable(property.Type)));
        }

        return Model(symbol, ParameterKind.QueryObject, wireName, new(properties.ToImmutable()));
    }

    private static IEnumerable<IPropertySymbol> GetQueryProperties(ITypeSymbol type)
    {
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property is { IsStatic: false, IsIndexer: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null })
                    yield return property;
            }
        }
    }

    private static ParameterModel Model(IParameterSymbol symbol, ParameterKind kind, string? wireName = null, in EquatableArray<QueryPropertyModel> queryProperties = default) =>
        new(symbol.Name, TypeFacts.Display(symbol.Type), kind, wireName ?? symbol.Name, TypeFacts.IsNullable(symbol.Type), DefaultValueFacts.Format(symbol), queryProperties);

    private static bool ValidateTemplate(AttributeSyntax attribute, string template, ImmutableArray<DiagnosticModel>.Builder diagnostics)
    {
        if (RouteTemplate.HasForbiddenSyntax(template))
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.ForbiddenRouteSyntax, attribute, template));

            return false;
        }

        var segment = RouteTemplate.FindNonSnakeCaseSegment(template);
        if (segment is not null)
        {
            diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.RouteSegmentNotSnakeCase, attribute, template, segment));

            return false;
        }

        return true;
    }

    private static bool ValidatePlaceholders(MethodDeclarationSyntax method, string route, in ImmutableArray<ParameterModel> parameters, ImmutableArray<DiagnosticModel>.Builder diagnostics)
    {
        var placeholders = RouteTemplate.GetPlaceholders(route);
        var routeParameters = parameters.Where(x => x.Kind == ParameterKind.Route).ToList();

        foreach (var placeholder in placeholders)
        {
            if (!routeParameters.Exists(x => string.Equals(x.WireName, placeholder, StringComparison.OrdinalIgnoreCase)))
            {
                diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.UnmatchedRoutePlaceholder, method.Identifier, method.Identifier.Text, placeholder));

                return false;
            }
        }

        foreach (var parameter in routeParameters)
        {
            if (!placeholders.Any(x => string.Equals(x, parameter.WireName, StringComparison.OrdinalIgnoreCase)))
            {
                diagnostics.Add(ApiModelParser.Diagnostic(Diagnostics.UnmatchedRoutePlaceholder, method.Identifier, method.Identifier.Text, parameter.Name));

                return false;
            }
        }

        return true;
    }

    private static List<(AttributeSyntax Attribute, string Method)> FindVerbs(MethodDeclarationSyntax method)
    {
        var result = new List<(AttributeSyntax, string)>();

        foreach (var attribute in method.AttributeLists.SelectMany(x => x.Attributes))
        {
            if (HttpMethodAttributes.TryGetValue(AttributeFacts.GetName(attribute), out var httpMethod))
                result.Add((attribute, httpMethod));
        }

        return result;
    }

    private static List<(AttributeSyntax Attribute, string Name)> FindBindingAttributes(ParameterSyntax parameter)
    {
        var result = new List<(AttributeSyntax, string)>();

        foreach (var attribute in parameter.AttributeLists.SelectMany(x => x.Attributes))
        {
            var name = AttributeFacts.GetName(attribute);
            if (BindingAttributeNames.Contains(name))
                result.Add((attribute, name));
        }

        return result;
    }

    private static string ToCamelCase(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);
}
