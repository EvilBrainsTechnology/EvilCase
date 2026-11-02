namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// A stretch of a text, marked where it is a web address.
/// </summary>
internal readonly record struct LinkSegment(string Text, bool Link);
