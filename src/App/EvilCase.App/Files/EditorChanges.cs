using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The Změny of an editor: whether they show, the number of the version the edited text compares with and its text once
/// read and comparable, the choice of that version, what stands in for the changes while they cannot show, and the
/// switch.
/// </summary>
public sealed record EditorChanges(bool Shown, int Number, string? Original, RenderFragment<string> Version, RenderFragment? Status, EventCallback<bool> OnShow);
