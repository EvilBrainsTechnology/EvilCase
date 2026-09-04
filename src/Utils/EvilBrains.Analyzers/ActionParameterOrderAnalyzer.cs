using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.Analyzers;

/// <summary>
/// An unattributed parameter ranks with the CancellationToken; EB1005 keeps every other one attributed.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ActionParameterOrderAnalyzer : DiagnosticAnalyzer
{
    private static readonly Dictionary<string, int> BindingRanks = new(StringComparer.Ordinal)
    {
        ["Microsoft.AspNetCore.Mvc.FromServicesAttribute"] = 0,
        ["Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute"] = 0,
        ["Microsoft.AspNetCore.Mvc.FromRouteAttribute"] = 1,
        ["Microsoft.AspNetCore.Mvc.FromQueryAttribute"] = 2,
        ["Microsoft.AspNetCore.Mvc.FromHeaderAttribute"] = 2,
        ["Microsoft.AspNetCore.Mvc.FromFormAttribute"] = 2,
        ["Microsoft.AspNetCore.Mvc.FromBodyAttribute"] = 3,
    };

    private const int TokenRank = 4;

    private static readonly DiagnosticDescriptor ParameterOutOfOrder = new(
        "EB0008",
        "Action parameter out of order",
        "Parameter '{0}' must come before '{1}': action parameters run [FromServices], [FromRoute], [FromQuery], [FromBody], CancellationToken",
        "EvilBrains.Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ParameterOutOfOrder];

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

        foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(MvcFacts.IsAction))
            AnalyzeAction(context, method);
    }

    private static void AnalyzeAction(in SymbolAnalysisContext context, IMethodSymbol method)
    {
        var highestRank = int.MinValue;
        var highestName = "";

        foreach (var parameter in method.Parameters)
        {
            var rank = GetParameterRank(parameter);
            if (rank < highestRank)
            {
                context.ReportDiagnostic(Diagnostic.Create(ParameterOutOfOrder, parameter.Locations[0], parameter.Name, highestName));

                return;
            }

            highestRank = rank;
            highestName = parameter.Name;
        }
    }

    private static int GetParameterRank(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            var name = attribute.AttributeClass?.ToDisplayString();
            if (name is not null && BindingRanks.TryGetValue(name, out var rank))
                return rank;
        }

        return TokenRank;
    }
}
