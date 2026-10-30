using EvilBrains.EvilCase.Business.Files;

namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// The names in a tenant export (SDD-022).
/// </summary>
internal static class TenantExportLayout
{
    /// <summary>
    /// Raised with every change of the structure, together with the schemas in docs/export.
    /// </summary>
    public const int FormatVersion = 1;

    public const string ManifestName = "export.json";

    public const string CasesFolder = "Spisy";

    public const string ContactsFolder = "Kontakty";

    public const string LabelsFolder = "Štítky";

    public const string CaseDocumentName = "!spis.json";

    public const string ActDocumentName = "!ukon.json";

    public const string LabelsDocumentName = "!stitky.json";

    public const string CaseFallback = "Spis";

    public const string ActFallback = "Úkon";

    private const string ContactFallback = "Kontakt";

    private const int MaxTitleLength = 80;

    public static string ArchiveName(DateTimeOffset started)
    {
        var date = TimeZoneInfo.ConvertTime(started, FileArchiveLayout.Prague).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return $"EvilCase export {date}.zip";
    }

    /// <summary>
    /// "yyyy-MM-dd Title" of a case or an act, before any collision suffix.
    /// </summary>
    public static string FolderName(DateOnly date, string title, string fallback)
    {
        return $"{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} {Title(title, fallback)}";
    }

    public static string ContactDocumentName(string name)
    {
        return Title(name, ContactFallback) + ".json";
    }

    // The cut can leave a space or a dot at the end, which a file system drops; the first character is neither.
    private static string Title(string title, string fallback)
    {
        var safe = FileArchiveLayout.SafeEntryName(title, fallback);

        return safe.Length > MaxTitleLength ? safe[..MaxTitleLength].TrimEnd(' ', '.') : safe;
    }
}
