using System.Text.RegularExpressions;
using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// The names in a file archive (SDD-012). Names are compared ignoring case, as the file systems an archive is
/// unpacked to compare them.
/// </summary>
internal static partial class FileArchiveLayout
{
    private const string EntryFallback = "soubor";

    public static string ArchiveName(string title, string fallback, bool includeVersions)
    {
        var name = FileNameRules.SafeFileName(title, fallback);

        return includeVersions ? $"{name} (včetně verzí).zip" : $"{name}.zip";
    }

    /// <summary>
    /// Each file under its name, its older versions beside it as "Name (verze N).ext" where they are included. The
    /// versions come in file order, those of each file by number. The current versions claim their names first, in
    /// file order, then the older ones; a name already claimed gets " (2)", " (3)" and on before its extension.
    /// </summary>
    public static List<FileArchiveEntry> Entries(IReadOnlyList<ArchivedVersion> versions, bool includeVersions)
    {
        var files = versions
            .GroupBy(static version => version.FileId)
            .Select(static file => file.ToList())
            .ToList();

        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = files.ConvertAll(static file => SafeEntryName(file[^1].FileName));
        var currentNames = names.ConvertAll(name => Claim(claimed, name));

        var entries = new List<FileArchiveEntry>();

        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];

            foreach (var older in includeVersions ? file.SkipLast(1) : [])
            {
                var number = older.Number.ToString(CultureInfo.InvariantCulture);

                entries.Add(Entry(older, Claim(claimed, Suffixed(names[index], $" (verze {number})"))));
            }

            entries.Add(Entry(file[^1], currentNames[index]));
        }

        return entries;
    }

    // No separator remains, and no run of dots another tool could read as a path step.
    private static string SafeEntryName(string fileName)
    {
        return DotRun.Replace(FileNameRules.SafeFileName(fileName, EntryFallback), ".");
    }

    private static string Claim(HashSet<string> claimed, string name)
    {
        var candidate = name;

        for (var copy = 2; !claimed.Add(candidate); copy++)
            candidate = Suffixed(name, $" ({copy.ToString(CultureInfo.InvariantCulture)})");

        return candidate;
    }

    private static string Suffixed(string name, string suffix)
    {
        var extension = Path.GetExtension(name);

        return name[..^extension.Length] + suffix + extension;
    }

    private static FileArchiveEntry Entry(ArchivedVersion version, string entryName)
    {
        return new FileArchiveEntry
        {
            EntryName = entryName,
            Created = version.Created,
            StoragePath = version.StoragePath,
        };
    }

    [GeneratedRegex(@"\.{2,}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DotRun { get; }
}
