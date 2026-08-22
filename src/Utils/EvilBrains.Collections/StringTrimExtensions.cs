namespace EvilBrains.Collections;

public static class StringTrimExtensions
{
    public static string? TrimEmptyToNull(this string str)
    {
        return string.IsNullOrWhiteSpace(str) ? null : str.Trim();
    }
}
