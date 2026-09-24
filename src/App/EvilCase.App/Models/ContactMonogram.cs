namespace EvilBrains.EvilCase.App.Models;

public static class ContactMonogram
{
    /// <summary>
    /// The first letters of the first two words of the name, upper-cased. A leading word ending in
    /// a period is a title and does not count as a word.
    /// </summary>
    public static string Text(string name)
    {
        var words = name
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .SkipWhile(static word => word.EndsWith('.'))
            .Take(2);

        return string.Concat(words.Select(static word => char.ToUpperInvariant(word[0])));
    }
}
