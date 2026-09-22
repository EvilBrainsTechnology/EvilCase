using EvilBrains.EvilCase.Domain.Labels;

namespace EvilBrains.EvilCase.App.Models;

public static class LabelColorDisplay
{
    public static IReadOnlyList<LabelColor> Palette { get; } = Enum.GetValues<LabelColor>();

    /// <summary>
    /// The Tabler colour name behind <c>bg-{name}</c> and <c>bg-{name}-lt</c>.
    /// </summary>
    public static string Css(LabelColor color)
    {
        return color switch
        {
            LabelColor.Blue => "blue",
            LabelColor.Azure => "azure",
            LabelColor.Indigo => "indigo",
            LabelColor.Purple => "purple",
            LabelColor.Pink => "pink",
            LabelColor.Red => "red",
            LabelColor.Orange => "orange",
            LabelColor.Yellow => "yellow",
            LabelColor.Lime => "lime",
            LabelColor.Green => "green",
            LabelColor.Teal => "teal",
            LabelColor.Cyan => "cyan",
            LabelColor.Gray => "gray",
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown label colour."),
        };
    }

    public static string Text(LabelColor color)
    {
        return color switch
        {
            LabelColor.Blue => "Modrá",
            LabelColor.Azure => "Blankytná",
            LabelColor.Indigo => "Indigová",
            LabelColor.Purple => "Fialová",
            LabelColor.Pink => "Růžová",
            LabelColor.Red => "Červená",
            LabelColor.Orange => "Oranžová",
            LabelColor.Yellow => "Žlutá",
            LabelColor.Lime => "Limetková",
            LabelColor.Green => "Zelená",
            LabelColor.Teal => "Tyrkysová",
            LabelColor.Cyan => "Azurová",
            LabelColor.Gray => "Šedá",
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown label colour."),
        };
    }
}
