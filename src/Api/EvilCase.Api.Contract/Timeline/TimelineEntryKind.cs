using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Api.Contract.Timeline;

/// <summary>
/// What one entry of a merged timeline is. Deadlines join this at M5.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TimelineEntryKind>))]
public enum TimelineEntryKind
{
    Act = 0,

    Comment = 1,
}
