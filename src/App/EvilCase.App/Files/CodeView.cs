namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// A code preview as shown: whether its text could be formatted, and whether its lines were cut at the line cap.
/// </summary>
internal sealed record CodeView(bool Formatted, bool LinesCut);
