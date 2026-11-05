using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The edit of a Markdown file in its preview: whether a save locks it, its Změny, the notes of its toolbar and what it
/// raises.
/// </summary>
public sealed record MarkdownEdit(
    bool Locked,
    EditorChanges Changes,
    RenderFragment Notes,
    EventCallback<bool> OnDirtyChanged,
    EventCallback OnSave,
    EventCallback OnUnavailable,
    EventCallback<string> OnLinkRequest);
