using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Domain.Labels;

/// <summary>
/// The palette the frontend paints a label with; the label carries no free colour.
/// </summary>
[JsonConverter(typeof(StrictJsonStringEnumConverter<LabelColor>))]
public enum LabelColor
{
    Blue = 0,

    Azure = 1,

    Indigo = 2,

    Purple = 3,

    Pink = 4,

    Red = 5,

    Orange = 6,

    Yellow = 7,

    Lime = 8,

    Green = 9,

    Teal = 10,

    Cyan = 11,

    Gray = 12,
}
