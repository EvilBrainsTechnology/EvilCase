using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EvilBrains.ApiClient.Generator;

/// <summary>
/// Orders an action's parameters: [FromServices], [FromRoute], [FromQuery], [FromBody], CancellationToken.
/// [FromHeader] and [FromForm] rank with [FromQuery]; EB1005 is what requires the binding attribute itself.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ActionParameterOrderAnalyzer : DiagnosticAnalyzer
{
    // A parameter without a binding attribute is a CancellationToken; EB1005 reports it when it is not.
    private const int TokenRank = 4;

    // Parameters of equal rank are free among themselves.
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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Diagnostics.ActionParameterOutOfOrder];

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

    // One report per action: the first parameter that sits behind a higher-ranked one.
    private static void AnalyzeAction(in SymbolAnalysisContext context, IMethodSymbol method)
    {
        var highestRank = int.MinValue;
        var highestName = "";

        foreach (var parameter in method.Parameters)
        {
            var rank = GetParameterRank(parameter);
            if (rank < highestRank)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ActionParameterOutOfOrder, parameter.Locations[0], parameter.Name, highestName));

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
