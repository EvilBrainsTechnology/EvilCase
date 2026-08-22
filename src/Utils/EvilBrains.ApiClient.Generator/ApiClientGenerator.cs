using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace EvilBrains.ApiClient.Generator;

/// <summary>
/// Generates HTTP client interfaces and implementations for [GenerateApiClient] controllers
/// found in controller sources passed to the consuming project as additional files.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ApiClientGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var rootNamespace = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
            provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var value) && !string.IsNullOrEmpty(value) ? value : null);

        var texts = context.AdditionalTextsProvider
            .Where(static x => x.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Collect();

        var model = context.CompilationProvider
            .Combine(context.ParseOptionsProvider)
            .Combine(texts)
            .Combine(rootNamespace)
            .Select(static (input, token) => Parse(input.Left.Left.Left, input.Left.Left.Right, input.Left.Right, input.Right, token));

        context.RegisterSourceOutput(
            model,
            static (production, apiModel) =>
        {
            foreach (var diagnostic in apiModel.Diagnostics)
                production.ReportDiagnostic(Diagnostics.Create(diagnostic));

            foreach (var client in apiModel.Clients)
                production.AddSource(client.Name + "Client.g.cs", ClientEmitter.EmitClient(apiModel.Namespace, client));

            if (apiModel.Clients.Count > 0)
                production.AddSource("ApiClientRegistrations.g.cs", ClientEmitter.EmitRegistrations(apiModel.Namespace, apiModel));
        });
    }

    private static ApiModel Parse(Compilation compilation, ParseOptions parseOptions, in ImmutableArray<AdditionalText> texts, string? rootNamespace, CancellationToken token)
    {
        return ApiModelParser.Parse(compilation, parseOptions, texts, rootNamespace ?? compilation.AssemblyName ?? "ApiClient", token);
    }
}
