using System.Text;
using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EditableTextTests
{
    private static readonly byte[] Mark = [0xEF, 0xBB, 0xBF];

    [Test]
    public void ValidUtf8IsReadWithoutAMark()
    {
        var editable = EditableText.ParseOrDefault("a\nb"u8)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editable.Text, Is.EqualTo("a\nb"));
            Assert.That(editable.ByteOrderMark, Is.False);
            Assert.That(editable.LineBreak, Is.EqualTo("\n"));
        }
    }

    [Test]
    public void TheByteOrderMarkIsLeftOutOfTheTextAndNoted()
    {
        var editable = EditableText.ParseOrDefault([.. Mark, .. "řádek"u8])!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editable.Text, Is.EqualTo("řádek"));
            Assert.That(editable.ByteOrderMark, Is.True);
        }
    }

    [Test]
    public void AnEmptyFileIsEditable()
    {
        var editable = EditableText.ParseOrDefault([])!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editable.Text, Is.Empty);
            Assert.That(editable.ByteOrderMark, Is.False);
            Assert.That(editable.LineBreak, Is.EqualTo("\n"));
        }
    }

    [TestCase(new byte[] { 0x50, 0xF8 })]
    [TestCase(new byte[] { 0x61, 0xC5 })]
    [TestCase(new byte[] { 0xC0, 0xAF })]
    public void ContentThatIsNotUtf8IsNotEditable(byte[] content)
    {
        Assert.That(EditableText.ParseOrDefault(content), Is.Null, "only valid UTF-8 is edited");
    }

    [TestCase("a\r\nb\nc", "\r\n")]
    [TestCase("a\nb\r\nc", "\n")]
    [TestCase("a\rb", "\r")]
    [TestCase("a\r", "\r")]
    [TestCase("abc", "\n")]
    public void TheFirstLineBreakDecides(string text, string lineBreak)
    {
        Assert.That(EditableText.ParseOrDefault(Encoding.UTF8.GetBytes(text))!.LineBreak, Is.EqualTo(lineBreak));
    }

    [Test]
    public void SavingWritesTheFilesLineBreaksAndMarkBack()
    {
        var editable = EditableText.ParseOrDefault([.. Mark, .. "a\r\nb\r\n"u8])!;
        byte[] expected = [.. Mark, .. "a\r\nc\r\nd\r\n"u8];

        Assert.That(editable.Encode("a\nc\nd\n"), Is.EqualTo(expected));
    }

    [TestCase("a\r\nb\r\n")]
    [TestCase("\uFEFFx\ny")]
    [TestCase("x\ry")]
    public void AnUnchangedTextSavesToItsOwnBytes(string original)
    {
        var bytes = Encoding.UTF8.GetBytes(original);
        var editable = EditableText.ParseOrDefault(bytes)!;
        var joined = editable.Text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        Assert.That(editable.Encode(joined), Is.EqualTo(bytes));
    }
}
