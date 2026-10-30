using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Text;

/// <summary>
/// Pages of <see cref="FileLimits.TextPageLength"/> characters, numbered from 1; an empty text is one empty page.
/// </summary>
internal static class FileTextPaging
{
    public static int PageCount(int length)
    {
        return length == 0 ? 1 : ((length - 1) / FileLimits.TextPageLength) + 1;
    }

    /// <summary>
    /// Null where the page lies outside the text.
    /// </summary>
    public static string? Page(string text, int page)
    {
        if (page < 1 || page > PageCount(text.Length))
            return null;

        var start = (page - 1) * FileLimits.TextPageLength;

        return text.Substring(start, Math.Min(FileLimits.TextPageLength, text.Length - start));
    }
}
