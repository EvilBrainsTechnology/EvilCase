namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// A code preview as shown: whether its text formatted, whether it is not valid in its language, and whether its lines
/// were cut at the line cap. A text neither formatted nor invalid failed to format for another reason.
/// </summary>
internal sealed record CodeView(bool Formatted, bool Invalid, bool LinesCut);
