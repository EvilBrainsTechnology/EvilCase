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

    /// <summary>
    /// The zone an entry's time is written in: a zip entry's time carries no zone, and an archive is unpacked in Czechia.
    /// </summary>
    public static TimeZoneInfo Prague { get; } = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

    /// <summary>
    /// The UTC moment as it stood on Prague clocks.
    /// </summary>
    public static DateTimeOffset PragueTime(DateTime utc)
    {
        return TimeZoneInfo.ConvertTime(new DateTimeOffset(utc.Ticks, TimeSpan.Zero), Prague);
    }

    public static string ArchiveName(string title, string fallback, bool includeVersions)
    {
        var name = FileNameRules.SafeFileName(title, fallback);

        return includeVersions ? $"{name} (včetně verzí).zip" : $"{name}.zip";
    }

    /// <summary>
    /// Each file under its name, its older versions beside it as "Name (verze N).ext" where they are included. The
    /// versions come in file order, those of each file by number. The current versions claim their names first, in
    /// file order, then the older ones; a name already claimed, also one in <paramref name="claimed"/>, gets " (2)",
    /// " (3)" and on before its extension.
    /// </summary>
    public static List<FileArchiveEntry> Entries(IReadOnlyList<ArchivedVersion> versions, bool includeVersions, HashSet<string>? claimed = null)
    {
        var files = versions
            .GroupBy(static version => version.FileId)
            .Select(static file => file.ToList())
            .ToList();

        claimed ??= ClaimedNames();
        var names = files.ConvertAll(static file => SafeEntryName(file[^1].FileName, EntryFallback));
        var currentNames = names.ConvertAll(name => ClaimFileName(claimed, name));

        var entries = new List<FileArchiveEntry>();

        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];

            foreach (var older in includeVersions ? file.SkipLast(1) : [])
            {
                var number = older.Number.ToString(CultureInfo.InvariantCulture);

                entries.Add(Entry(older, ClaimFileName(claimed, Suffixed(names[index], $" (verze {number})"))));
            }

            entries.Add(Entry(file[^1], currentNames[index]));
        }

        return entries;
    }

    /// <summary>
    /// The names one folder of an archive holds.
    /// </summary>
    public static HashSet<string> ClaimedNames()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The folder's name, or where it is claimed already, the name with " (2)", " (3)" and on at its end.
    /// </summary>
    public static string ClaimFolderName(HashSet<string> claimed, string name)
    {
        return Claim(claimed, name, static (folder, suffix) => folder + suffix);
    }

    /// <summary>
    /// The file's name, or where it is claimed already, the name with " (2)", " (3)" and on before its extension.
    /// </summary>
    public static string ClaimFileName(HashSet<string> claimed, string name)
    {
        return Claim(claimed, name, Suffixed);
    }

    /// <summary>
    /// No separator remains, and no run of dots another tool could read as a path step.
    /// </summary>
    public static string SafeEntryName(string name, string fallback)
    {
        return DotRun.Replace(FileNameRules.SafeFileName(name, fallback), ".");
    }

    private static string Claim(HashSet<string> claimed, string name, Func<string, string, string> suffixed)
    {
        var candidate = name;

        for (var copy = 2; !claimed.Add(candidate); copy++)
            candidate = suffixed(name, $" ({copy.ToString(CultureInfo.InvariantCulture)})");

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
            Created = PragueTime(version.Created),
            StoragePath = version.StoragePath,
        };
    }

    [GeneratedRegex(@"\.{2,}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DotRun { get; }
}
