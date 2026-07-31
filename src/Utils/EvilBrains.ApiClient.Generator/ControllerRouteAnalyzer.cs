using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.ApiClient.Generator;

/// <summary>
/// Enforces route conventions on [ApiController] classes: [Route] is mandatory on the controller,
/// each action carries exactly one HTTP method attribute with a template, and templates are relative kebab-case.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ControllerRouteAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Diagnostics.MissingControllerRoute,
            Diagnostics.MissingActionRoute,
            Diagnostics.ForbiddenRouteSyntax,
            Diagnostics.RouteSegmentNotKebabCase);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (!MvcFacts.IsApiController(type))
            return;

        AnalyzeControllerRoute(context, type);

        foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(MvcFacts.IsAction))
            AnalyzeActionRoute(context, method);
    }

    private static void AnalyzeControllerRoute(in SymbolAnalysisContext context, INamedTypeSymbol type)
    {
        var route = MvcFacts.FindRouteAttribute(type);
        var template = route is null ? null : MvcFacts.GetTemplate(route);
        if (template is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingControllerRoute, type.Locations[0], type.Name));

            return;
        }

        ValidateTemplate(context, MvcFacts.GetLocation(route!, type), template);
    }

    private static void AnalyzeActionRoute(in SymbolAnalysisContext context, IMethodSymbol method)
    {
        var verbs = method.GetAttributes().Where(MvcFacts.IsHttpMethodAttribute).ToList();
        var template = verbs.Count == 1 ? MvcFacts.GetTemplate(verbs[0]) : null;
        if (template is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingActionRoute, method.Locations[0], method.Name));

            return;
        }

        ValidateTemplate(context, MvcFacts.GetLocation(verbs[0], method), template);
    }

    private static void ValidateTemplate(in SymbolAnalysisContext context, Location location, string template)
    {
        if (RouteTemplate.HasForbiddenSyntax(template))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ForbiddenRouteSyntax, location, template));

            return;
        }

        var segment = RouteTemplate.FindNonKebabCaseSegment(template);
        if (segment is not null)
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.RouteSegmentNotKebabCase, location, template, segment));
    }
}
