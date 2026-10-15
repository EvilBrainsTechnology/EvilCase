namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// The icon a file gets; it follows the extension alone.
/// </summary>
public enum FileKind
{
    Pdf = 0,

    Document = 1,

    Sheet = 2,

    Presentation = 3,

    Text = 4,

    Image = 5,

    Audio = 6,

    Video = 7,

    Email = 8,

    DataMessage = 9,

    Signature = 10,

    Code = 11,

    Archive = 12,

    Other = 13,
}
