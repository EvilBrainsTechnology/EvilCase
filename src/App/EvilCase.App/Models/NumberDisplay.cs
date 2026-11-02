namespace EvilBrains.EvilCase.App.Models;

internal static class NumberDisplay
{
    private static readonly NumberFormatInfo Czech = NumberFormatInfo.ReadOnly(new NumberFormatInfo { NumberGroupSeparator = "\u00A0" });

    /// <summary>
    /// A count with its thousands grouped by a non-breaking space: "1 234 567".
    /// </summary>
    public static string Text(long count)
    {
        return count.ToString("N0", Czech);
    }
}
