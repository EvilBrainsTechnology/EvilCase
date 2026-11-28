using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.App.Models;

internal static class CaseFileTreeDisplay
{
    /// <summary>
    /// The files of the case's acts and of every case below it.
    /// </summary>
    public static int FileCount(CaseFileTree tree)
    {
        return tree.Acts.Sum(static act => act.Files.Count) + tree.SubCases.Sum(FileCount);
    }

    /// <summary>
    /// "26 souborů v 6 úkonech": the files of a case tree and the acts holding them.
    /// </summary>
    public static string TreeText(int fileCount, int actCount)
    {
        return $"{FilesText(fileCount)} v {Plural.Text(actCount, "úkonu", "úkonech", "úkonech")}";
    }

    /// <summary>
    /// The facts after a case row's date: "2 úkony · 1 podspis · 5 souborů"; no acts or sub-cases leave out their part.
    /// </summary>
    public static string CaseFacts(CaseFileTree tree)
    {
        List<string> parts = [];

        if (tree.Acts.Count > 0)
            parts.Add(Plural.Text(tree.Acts.Count, "úkon", "úkony", "úkonů"));

        if (tree.SubCases.Count > 0)
            parts.Add(Plural.Text(tree.SubCases.Count, "podspis", "podspisy", "podspisů"));

        parts.Add(FilesText(FileCount(tree)));

        return string.Join(" · ", parts);
    }

    /// <summary>
    /// The facts after an act row's date: its contact, where it has one, and its files.
    /// </summary>
    public static string ActFacts(ActFileTree act)
    {
        var files = FilesText(act.Files.Count);

        return string.IsNullOrEmpty(act.Act.ContactName) ? files : $"{act.Act.ContactName} · {files}";
    }

    public static string FilesText(int count)
    {
        return count == 0 ? "bez souborů" : Plural.Text(count, "soubor", "soubory", "souborů");
    }
}
