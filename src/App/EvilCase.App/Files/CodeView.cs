namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// How a read-only view formatted its code: whether the text formatted and whether it is not valid in its language. A
/// text asked to format that is neither failed for another reason.
/// </summary>
public sealed record CodeView(bool Formatted, bool Invalid);
