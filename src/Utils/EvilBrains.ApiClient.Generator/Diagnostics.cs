using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace EvilBrains.ApiClient.Generator;

/// <summary>
/// EB1001-EB1006 are convention rules enforced by analyzers in the API project (and re-checked by the generator);
/// EB1010+ are client-feasibility rules reported only by the generator.
/// </summary>
internal static class Diagnostics
{
    private const string Category = "EvilBrains.ApiClient";

    public static readonly DiagnosticDescriptor MissingControllerRoute =
        Descriptor("EB1001", "Missing controller route", "Controller '{0}' must declare a [Route] attribute with a route template (empty template is allowed)");

    public static readonly DiagnosticDescriptor MissingActionRoute =
        Descriptor("EB1002", "Missing action route", "Action '{0}' must have exactly one HTTP method attribute with a route template (empty template is allowed)");

    public static readonly DiagnosticDescriptor ForbiddenRouteSyntax =
        Descriptor("EB1003", "Forbidden route syntax", "Route template '{0}' must not start with '/' or '~' and must not contain tokens, catch-all or empty placeholders");

    public static readonly DiagnosticDescriptor RouteSegmentNotKebabCase =
        Descriptor("EB1004", "Route segment not kebab-case", "Route segment '{1}' of template '{0}' must be kebab-case");

    public static readonly DiagnosticDescriptor MissingBindingAttribute =
        Descriptor("EB1005", "Missing binding attribute", "Parameter '{0}' must have exactly one binding attribute or be a CancellationToken");

    public static readonly DiagnosticDescriptor MissingApiRoutePrefix =
        Descriptor("EB1006", "Missing API route prefix", "Controller route template '{0}' must open with the '" + RouteTemplate.ApiPrefix + "' segment; it is what separates the API from everything else the host serves");

    // EB1010+ are reported by the source generator only; analyzer release tracking (RS2000) covers DiagnosticAnalyzer rules.
#pragma warning disable RS2000
    public static readonly DiagnosticDescriptor UnmatchedRoutePlaceholder =
        Descriptor("EB1010", "Unmatched route placeholder", "Route placeholder and [FromRoute] parameters of action '{0}' must match ('{1}' has no counterpart)");

    public static readonly DiagnosticDescriptor DuplicateSpecialParameter =
        Descriptor("EB1011", "Duplicate body or CancellationToken parameter", "Action '{0}' has multiple [FromBody] or CancellationToken parameters");

    public static readonly DiagnosticDescriptor UnsupportedParameter =
        Descriptor("EB1012", "Unsupported parameter", "Parameter '{0}' is not supported for client generation");

    public static readonly DiagnosticDescriptor ParameterNotSimple =
        Descriptor("EB1013", "Parameter type not simple", "Parameter '{0}' must be a simple type (route parameters also non-nullable)");

    public static readonly DiagnosticDescriptor TypeNotVisibleToClient =
        Descriptor("EB1014", "Type not visible to the client", "Type '{0}' is not resolvable in the client compilation; move it to the shared contract assembly");

    public static readonly DiagnosticDescriptor QueryPropertyNotSimple =
        Descriptor("EB1015", "Query property not simple", "Property '{0}' of [FromQuery] parameter type '{1}' must be a simple type");

    public static readonly DiagnosticDescriptor DuplicateClientName =
        Descriptor("EB1016", "Duplicate client name", "Client name '{0}' is generated from multiple controllers");
#pragma warning restore RS2000

    private static readonly Dictionary<string, DiagnosticDescriptor> Descriptors = CreateDescriptorIndex();

    public static Diagnostic Create(DiagnosticModel model)
    {
        var location = model.Location is null ? Location.None : model.Location.ToLocation();

        return Diagnostic.Create(Descriptors[model.Id], location, model.Arguments.Cast<object>().ToArray());
    }

    private static Dictionary<string, DiagnosticDescriptor> CreateDescriptorIndex()
    {
        var descriptors = new DiagnosticDescriptor[]
        {
            MissingControllerRoute,
            MissingActionRoute,
            ForbiddenRouteSyntax,
            RouteSegmentNotKebabCase,
            MissingBindingAttribute,
            MissingApiRoutePrefix,
            UnmatchedRoutePlaceholder,
            DuplicateSpecialParameter,
            UnsupportedParameter,
            ParameterNotSimple,
            TypeNotVisibleToClient,
            QueryPropertyNotSimple,
            DuplicateClientName,
        };

        return descriptors.ToDictionary(x => x.Id, StringComparer.Ordinal);
    }

    private static DiagnosticDescriptor Descriptor(string id, string title, string messageFormat)
    {
        return new(id, title, messageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
    }
}
