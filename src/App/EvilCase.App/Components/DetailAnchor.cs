namespace EvilBrains.EvilCase.App.Components;

/// <summary>
/// One chip of <see cref="DetailAnchors"/>: the section's name, its id and its count, read at each render; no count while it loads or after its load failed.
/// </summary>
public sealed record DetailAnchor(string Text, string Target, Func<int?> Count);
