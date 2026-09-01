using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EvilBrains.Utils.Tests.ApiClient;

internal static class TestCompilation
{
    private static readonly ImmutableArray<MetadataReference> References = LoadReferences();

    public static CSharpCompilation Create(string assemblyName, params string[] sources)
    {
        var trees = sources.Select(static x => CSharpSyntaxTree.ParseText(x, new CSharpParseOptions(LanguageVersion.Latest))).ToArray();

        return CSharpCompilation.Create(
            assemblyName,
            trees,
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static ImmutableArray<MetadataReference> LoadReferences()
    {
        var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var references = trusted
            .Split(Path.PathSeparator)
            .Append(typeof(EvilBrains.ApiClient.GenerateApiClientAttribute).Assembly.Location)
            .Distinct()
            .Select(static x => (MetadataReference)MetadataReference.CreateFromFile(x));

        return [.. references];
    }
}
