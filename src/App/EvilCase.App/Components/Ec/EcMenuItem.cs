using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Components.Ec;

public sealed record EcMenuItem
{
    public required string Text { get; init; }

    public required string Icon { get; init; }

    public required EventCallback OnSelect { get; init; }

    public bool Danger { get; init; }

    public bool DividerBefore { get; init; }
}
