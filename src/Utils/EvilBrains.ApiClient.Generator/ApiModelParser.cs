using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EvilBrains.ApiClient.Generator;

/// <summary>
/// Sources join a fork of the client compilation: contract types resolve, MVC attributes do not and are read by name.
/// </summary>
internal static class ApiModelParser
{
    private const string GenerateApiClientAttributeName = "GenerateApiClient";

    private const string ControllerSuffix = "Controller";

    public static ApiModel Parse(Compilation compilation, ParseOptions parseOptions, in ImmutableArray<AdditionalText> texts, string rootNamespace, CancellationToken token)
    {
        var clients = ImmutableArray.CreateBuilder<ClientModel>();
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticModel>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        var trees = ParseTrees(parseOptions, texts, token);
        var fork = compilation.AddSyntaxTrees(trees);

        foreach (var tree in trees)
        {
            SemanticModel? semanticModel = null;

            foreach (var controller in tree.GetRoot(token).DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                token.ThrowIfCancellationRequested();

                if (!AttributeFacts.Has(controller.AttributeLists, GenerateApiClientAttributeName))
                    continue;

                var name = GetClientName(controller);
                if (!names.Add(name))
                {
                    diagnostics.Add(Diagnostic(Diagnostics.DuplicateClientName, controller.Identifier, name));

                    continue;
                }

                semanticModel ??= fork.GetSemanticModel(tree);
                var client = ControllerParser.Parse(controller, semanticModel, name, diagnostics);
                if (client is not null)
                    clients.Add(client);
            }
        }

        return new(rootNamespace, new(clients.ToImmutable()), new(diagnostics.ToImmutable()));
    }

    public static DiagnosticModel Diagnostic(DiagnosticDescriptor descriptor, SyntaxNode node, params string[] arguments)
    {
        return new(descriptor.Id, LocationModel.FromNode(node), new(ImmutableArray.Create(arguments)));
    }

    public static DiagnosticModel Diagnostic(DiagnosticDescriptor descriptor, in SyntaxToken token, params string[] arguments)
    {
        return new(descriptor.Id, LocationModel.FromToken(token), new(ImmutableArray.Create(arguments)));
    }

    private static ImmutableArray<SyntaxTree> ParseTrees(ParseOptions parseOptions, in ImmutableArray<AdditionalText> texts, CancellationToken token)
    {
        var trees = ImmutableArray.CreateBuilder<SyntaxTree>();

        foreach (var text in texts)
        {
            var source = text.GetText(token);
            if (source is null)
                continue;

            trees.Add(CSharpSyntaxTree.ParseText(source, (CSharpParseOptions)parseOptions, text.Path, cancellationToken: token));
        }

        return trees.ToImmutable();
    }

    private static string GetClientName(ClassDeclarationSyntax controller)
    {
        var name = controller.Identifier.Text;

        return name.EndsWith(ControllerSuffix, StringComparison.Ordinal) ? name.Substring(0, name.Length - ControllerSuffix.Length) : name;
    }
}
