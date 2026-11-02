namespace EvilBrains.EvilCase.App.Models;

internal static class TextDisplay
{
    public static string OrDash(string text)
    {
        return text.Length == 0 ? "—" : text;
    }
}
