namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// The language a code preview or the source editor highlights a text in; an ISDOC invoice is XML. Markdown is
/// highlighted only in the source editor.
/// </summary>
public enum CodeLanguage
{
    Json = 0,

    Xml = 1,

    Yaml = 2,

    Html = 3,

    Markdown = 4,
}
