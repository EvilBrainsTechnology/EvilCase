using System.Text;
using System.Text.RegularExpressions;

namespace EvilBrains.EvilCase.Import;

/// <summary>
/// Reads a folder tree as a case tree. Names carry the structure — which folder, which act, which
/// attachment — and nothing else about a file is decided here; that comes from its bytes
/// (<see cref="FileContentClassifier"/>).
/// </summary>
/// <remarks>
/// Pure, and it never touches a filesystem: it takes the tree as <see cref="FolderNode"/> data, so it
/// cannot write to the source it is reading.
/// </remarks>
public static partial class CaseFolderParser
{
    /// <summary>
    /// Generated summaries. Ignored entirely — not an act, not a file, not a problem.
    /// </summary>
    public const int SummaryOrdinal = 99;

    private const string ClosedMarker = "(uzavreno)";

    public static ImportedTree Parse(FolderNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var problems = new List<ImportProblem>();

        return new()
        {
            Root = ParseFolder(root, root.Name, problems),
            Problems = problems,
        };
    }

    private static ImportedCase ParseFolder(FolderNode folder, string path, List<ImportProblem> problems)
    {
        var byOrdinal = new SortedDictionary<int, List<ImportedFile>>();

        foreach (var fileName in folder.Files)
        {
            var match = FileNamePattern.Match(fileName);

            if (!match.Success)
            {
                problems.Add(new() { Path = $"{path}/{fileName}", Reason = "the name does not start with an act number" });
                continue;
            }

            var ordinal = int.Parse(match.Groups["ordinal"].ValueSpan, CultureInfo.InvariantCulture);

            if (ordinal == SummaryOrdinal)
                continue;

            if (!byOrdinal.TryGetValue(ordinal, out var files))
            {
                files = [];
                byOrdinal[ordinal] = files;
            }

            files.Add(new()
            {
                Name = fileName,
                IsAttachment = match.Groups["marker"].Success,
                Title = Path.GetFileNameWithoutExtension(match.Groups["rest"].Value),
            });
        }

        var (title, isClosed) = ReadFolderName(folder.Name);

        return new()
        {
            Title = title,
            IsClosed = isClosed,
            Acts = [.. byOrdinal.Select(ToAct)],
            SubCases = [.. folder.Folders.Select(child => ParseFolder(child, $"{path}/{child.Name}", problems))],
        };
    }

    private static ImportedAct ToAct(KeyValuePair<int, List<ImportedFile>> ordinal)
    {
        // By name, because the attachment's own number lives in free text the parser never reads.
        var files = ordinal.Value.OrderBy(file => file.Name, StringComparer.Ordinal).ToList();
        var titled = files.Find(file => !file.IsAttachment) ?? files[0];

        return new()
        {
            Ordinal = ordinal.Key,
            Title = titled.Title,
            Files = files,
        };
    }

    /// <summary>
    /// Splits a closed folder's name from its marker. Matched on the last parenthesis rather than by
    /// cutting a fixed length, because folding diacritics changes how long the string is.
    /// </summary>
    private static (string Title, bool IsClosed) ReadFolderName(string name)
    {
        var marker = name.LastIndexOf('(');

        return marker > 0 && string.Equals(Fold(name[marker..]), ClosedMarker, StringComparison.Ordinal)
            ? (name[..marker].TrimEnd(), true)
            : (name.Trim(), false);
    }

    private static string Fold(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var folded = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                _ = folded.Append(char.ToLowerInvariant(character));
        }

        return folded.ToString();
    }

    // The letter after the number is the attachment marker. Whatever word follows the dash - Priloha,
    // Attachment, anything - is title text, so no literal from the convention appears here.
    [GeneratedRegex(@"^(?<ordinal>\d{1,4})(?<marker>[A-Za-z])?\s*-\s*(?<rest>.+)$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex FileNamePattern { get; }
}
