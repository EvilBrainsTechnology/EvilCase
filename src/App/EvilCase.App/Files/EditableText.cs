using System.Text;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The text of a file as the editor takes it: valid UTF-8 without the byte order mark, with the mark and the line break
/// noted so that <see cref="Encode"/> writes them back.
/// </summary>
internal sealed record EditableText(string Text, bool ByteOrderMark, string LineBreak)
{
    private static readonly UTF8Encoding Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// The text of the content, or <see langword="null"/> when the content is not valid UTF-8.
    /// </summary>
    public static EditableText? ParseOrDefault(ReadOnlySpan<byte> content)
    {
        var byteOrderMark = content.StartsWith(Encoding.UTF8.Preamble);
        string text;

        try
        {
            text = Strict.GetString(byteOrderMark ? content[Encoding.UTF8.Preamble.Length..] : content);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }

        return new EditableText(text, byteOrderMark, LineBreakOf(text));
    }

    /// <summary>
    /// The bytes of the edited text, whose lines the editor joins with "\n", with the file's mark and line break.
    /// </summary>
    public byte[] Encode(string edited)
    {
        var bytes = Encoding.UTF8.GetBytes(string.Equals(this.LineBreak, "\n", StringComparison.Ordinal) ? edited : edited.Replace("\n", this.LineBreak, StringComparison.Ordinal));

        return this.ByteOrderMark ? [.. Encoding.UTF8.Preamble, .. bytes] : bytes;
    }

    // The first line break of the text decides; a text without one takes "\n".
    private static string LineBreakOf(string text)
    {
        var index = text.AsSpan().IndexOfAny('\r', '\n');

        if (index < 0 || text[index] == '\n')
            return "\n";

        return index + 1 < text.Length && text[index + 1] == '\n' ? "\r\n" : "\r";
    }
}
