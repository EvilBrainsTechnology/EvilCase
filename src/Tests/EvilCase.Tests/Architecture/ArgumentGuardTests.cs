namespace EvilBrains.EvilCase.Tests.Architecture;

/// <summary>
/// Nullability is the guard. A runtime null check on a parameter the compiler keeps non-null is code no
/// call can reach, so the solution's sources carry none.
/// </summary>
public class ArgumentGuardTests
{
    private const string SolutionFileName = "EvilCase.slnx";

    private static readonly string[] SourceExtensions = [".cs", ".razor"];

    private static readonly string[] BuildOutputFolders = ["bin", "obj"];

    // Composed rather than written out, so this file is not its own counterexample.
    private static readonly string Guard = $"{nameof(ArgumentNullException)}.ThrowIfNull";

    private static readonly string SourceRoot = FindSourceRoot();

    private static IReadOnlyList<string> SourceFiles => SourceExtensions
        .SelectMany(extension => Directory.EnumerateFiles(SourceRoot, $"*{extension}", SearchOption.AllDirectories))
        .Select(path => Path.GetRelativePath(SourceRoot, path))
        .Where(path => SourceExtensions.Contains(Path.GetExtension(path), StringComparer.Ordinal) && !IsBuildOutput(path))
        .Order(StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// Without this the check below would pass on an empty source tree.
    /// </summary>
    [Test]
    public void TheSourceTreeIsTheOneUnderTest()
    {
        Assert.That(
            SourceFiles,
            Is.Not.Empty,
            "the solution's sources must be reachable from the test output, or nothing here is being asserted");
    }

    [Test]
    public void NothingGuardsAParameterThatCannotBeNull()
    {
        var offenders = SourceFiles
            .Where(path => File.ReadAllText(Path.Combine(SourceRoot, path)).Contains(Guard, StringComparison.Ordinal))
            .ToList();

        Assert.That(offenders, Is.Empty, $"{Guard} must not stand where the parameter is non-nullable");
    }

    /// <summary>
    /// The tests run out of a build output folder inside the tree they check, so the solution file is above them.
    /// </summary>
    private static string FindSourceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            directory = directory.Parent;

        return directory?.FullName ?? AppContext.BaseDirectory;
    }

    private static bool IsBuildOutput(string relativePath) => relativePath
        .Split(Path.DirectorySeparatorChar)
        .Any(segment => BuildOutputFolders.Contains(segment, StringComparer.Ordinal));
}
