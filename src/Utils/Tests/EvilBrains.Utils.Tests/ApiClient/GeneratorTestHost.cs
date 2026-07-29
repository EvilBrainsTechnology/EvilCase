using System.Collections.Immutable;
using EvilBrains.ApiClient.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EvilBrains.Utils.Tests.ApiClient;

internal static class GeneratorTestHost
{
    public const string ControllerPath = @"C:\FakeApi\Controllers\ItemsController.cs";

    private const string GlobalUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    public static (ImmutableArray<Diagnostic> Diagnostics, Compilation Output) Run(string controllerSource, string? contractSource = null)
    {
        string[] sources = contractSource is null ? [GlobalUsings] : [GlobalUsings, contractSource];
        var compilation = TestCompilation.Create("FakeApi.Client", sources);

        var driver = CSharpGeneratorDriver.Create(
            [new ApiClientGenerator().AsSourceGenerator()],
            additionalTexts: [new TestAdditionalText(ControllerPath, controllerSource)],
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest));

        _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        return (diagnostics, output);
    }
}
