namespace EvilBrains.EvilCase.App.Models;

public static class ContactMonogram
{
    /// <summary>
    /// The first letters of the first two words of the name, upper-cased.
    /// </summary>
    public static string Text(string name)
    {
        var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        return string.Concat(words.Take(2).Select(static word => char.ToUpperInvariant(word[0])));
    }
}
